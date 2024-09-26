# Global variable passed as string, convert to boolean
$Force = "true"  # Replace this with the actual value passed
$Force = [bool]::Parse($Force)

# Define separate caches for AD user, group checks, and group members
$userCheckCache = @{}
$groupCheckCache = @{}
$groupMembersCacheBefore = @{}
$groupMembersCacheAfter = @{}

# Function to check if a user exists in AD
function Check-ADUserExists {
    param($NTLoginId)
    try {
        $user = Get-ADUser -Identity $NTLoginId -ErrorAction Stop
        return $true
    } catch {
        Write-Warning "Error: User $NTLoginId not found or AD query failed. $_"
        return $false
    }
}

# Function to check if a group exists in AD
function Check-ADGroupExists {
    param($AdGroupName)
    try {
        $group = Get-ADGroup -Identity $AdGroupName -ErrorAction Stop
        return $true
    } catch {
        Write-Warning "Error: Group $AdGroupName not found or AD query failed. $_"
        return $false
    }
}

# Function to get group members
function Get-ADGroupMembers {
    param($AdGroupName)
    try {
        $members = Get-ADGroupMember -Identity $AdGroupName -ErrorAction Stop
        return $members | ForEach-Object { $_.SamAccountName }
    } catch {
        Write-Warning "Error: Failed to retrieve members for group $AdGroupName. $_"
        return @()
    }
}

# Function to add multiple users to an AD group
function Add-UsersToGroup {
    param($NTLoginIds, $AdGroupName)
    
    try {
        Add-ADGroupMember -Identity $AdGroupName -Members $NTLoginIds -ErrorAction Stop
        Write-Host "Successfully added users: $($NTLoginIds -join ', ') to group $AdGroupName."
    } catch {
        Write-Warning "Error: Failed to add users: $($NTLoginIds -join ', ') to group $AdGroupName. $_"
    }
}

# Function to remove multiple users from an AD group
function Remove-UsersFromGroup {
    param($NTLoginIds, $AdGroupName)
    
    try {
        Remove-ADGroupMember -Identity $AdGroupName -Members $NTLoginIds -Confirm:$false -ErrorAction Stop
        Write-Host "Successfully removed users: $($NTLoginIds -join ', ') from group $AdGroupName."
    } catch {
        Write-Warning "Error: Failed to remove users: $($NTLoginIds -join ', ') from group $AdGroupName. $_"
    }
}

# Function to verify if users were successfully added or removed
function Verify-GroupMembers {
    param($NTLoginIds, $AdGroupName, $Operation)
    $results = @()

    # Get the updated group members
    $updatedMembers = Get-ADGroupMembers -AdGroupName $AdGroupName
    
    foreach ($NTLoginId in $NTLoginIds) {
        $verificationResult = $false
        if ($Operation -eq "Add") {
            if ($NTLoginId -in $updatedMembers) {
                $verificationResult = $true
            }
        } elseif ($Operation -eq "Remove") {
            if ($NTLoginId -notin $updatedMembers) {
                $verificationResult = $true
            }
        }
        # Store the verification result
        $results += [pscustomobject]@{
            AdGroupName = $AdGroupName
            NTLoginId   = $NTLoginId
            Success     = $verificationResult
        }
    }
    return $results
}

# Sample array of objects
$items = @(
    @{ NTLoginId = 'user1'; AdGroupName = 'group1'; ApprovalStatus = 'Approved' },
    @{ NTLoginId = 'user2'; AdGroupName = 'group1'; ApprovalStatus = 'Canceled' },
    @{ NTLoginId = 'user3'; AdGroupName = 'group2'; ApprovalStatus = 'Suspended' },
    @{ NTLoginId = 'user4'; AdGroupName = 'group2'; ApprovalStatus = 'Completed' }
)

# Step 1: Pre-operation - Retrieve members of each group and cache them
$groups = $items | Select-Object -ExpandProperty AdGroupName -Unique
foreach ($group in $groups) {
    $groupMembersCacheBefore[$group] = Get-ADGroupMembers -AdGroupName $group
}

# Create collections to hold grouped users for add and remove operations
$usersToAdd = @{}
$usersToRemove = @{}

# Step 2: Process each item and prepare users for adding/removing
foreach ($item in $items) {
    $NTLoginId = $item.NTLoginId
    $AdGroupName = $item.AdGroupName
    $ApprovalStatus = $item.ApprovalStatus

    # Check if user existence is cached
    if (-not $userCheckCache.ContainsKey($NTLoginId)) {
        $userExists = Check-ADUserExists $NTLoginId
        $userCheckCache[$NTLoginId] = $userExists
    }

    # Check if group existence is cached
    if (-not $groupCheckCache.ContainsKey($AdGroupName)) {
        $groupExists = Check-ADGroupExists $AdGroupName
        $groupCheckCache[$AdGroupName] = $groupExists

        # If group exists, cache the group members
        if ($groupExists) {
            $groupMembersCacheBefore[$AdGroupName] = Get-ADGroupMembers $AdGroupName
        }
    }

    # Retrieve cached results
    $userExists = $userCheckCache[$NTLoginId]
    $groupExists = $groupCheckCache[$AdGroupName]
    $groupMembers = $groupMembersCacheBefore[$AdGroupName]

    # Only proceed if both the user and group exist
    if ($userExists -and $groupExists) {
        if (($ApprovalStatus -eq 'Approved' -or ($ApprovalStatus -eq 'Completed' -and $Force)) -or
            ($ApprovalStatus -eq 'Suspended' -and $Force)) {
            if ($groupMembers -notcontains $NTLoginId) {
                if (-not $usersToAdd.ContainsKey($AdGroupName)) {
                    $usersToAdd[$AdGroupName] = @()
                }
                $usersToAdd[$AdGroupName] += $NTLoginId
            }
        } elseif (($ApprovalStatus -eq 'Canceled') -or ($ApprovalStatus -eq 'Suspended' -and -not $Force)) {
            if ($groupMembers -contains $NTLoginId) {
                if (-not $usersToRemove.ContainsKey($AdGroupName)) {
                    $usersToRemove[$AdGroupName] = @()
                }
                $usersToRemove[$AdGroupName] += $NTLoginId
            }
        }
    } else {
        Write-Host "Either user $NTLoginId or group $AdGroupName does not exist in AD."
    }
}

# Step 3: Perform add and remove operations
foreach ($groupName in $usersToAdd.Keys) {
    $users = $usersToAdd[$groupName]
    if ($users.Count -gt 0) {
        Add-UsersToGroup -NTLoginIds $users -AdGroupName $groupName
    }
}

foreach ($groupName in $usersToRemove.Keys) {
    $users = $usersToRemove[$groupName]
    if ($users.Count -gt 0) {
        Remove-UsersFromGroup -NTLoginIds $users -AdGroupName $groupName
    }
}

# Step 4: Post-operation - Retrieve members of each group and verify
foreach ($group in $groups) {
    $groupMembersCacheAfter[$group] = Get-ADGroupMembers -AdGroupName $group
}

# Step 5: Verification and collecting results for database update
$verificationResults = @()
foreach ($groupName in $usersToAdd.Keys) {
    $users = $usersToAdd[$groupName]
    $verificationResults += Verify-GroupMembers -NTLoginIds $users -AdGroupName $groupName -Operation "Add"
}

foreach ($groupName in $usersToRemove.Keys) {
    $users = $usersToRemove[$groupName]
    $verificationResults += Verify-GroupMembers -NTLoginIds $users -AdGroupName $groupName -Operation "Remove"
}

# Now $verificationResults contains the data with GroupName, NTLoginId, and Success boolean
# You can loop through this and call your stored procedure for each result

foreach ($result in $verificationResults) {
    $AdGroupName = $result.AdGroupName
    $NTLoginId = $result.NTLoginId
    $Success = $result.Success

    # Call your stored procedure here with $AdGroupName, $NTLoginId, and $Success
    # Example: Execute your stored procedure using Invoke-SqlCmd or your preferred method
    Write-Host "Updating database with Group: $AdGroupName, User: $NTLoginId, Success: $Success"
}