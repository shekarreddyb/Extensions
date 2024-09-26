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
        # Add multiple users to the AD group
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
        # Remove multiple users from the AD group
        Remove-ADGroupMember -Identity $AdGroupName -Members $NTLoginIds -Confirm:$false -ErrorAction Stop
        Write-Host "Successfully removed users: $($NTLoginIds -join ', ') from group $AdGroupName."
    } catch {
        Write-Warning "Error: Failed to remove users: $($NTLoginIds -join ', ') from group $AdGroupName. $_"
    }
}

# Function to verify if users were successfully added or removed
function Verify-GroupMembers {
    param($NTLoginIds, $AdGroupName, $Operation)
    
    # Get the updated group members
    $updatedMembers = Get-ADGroupMembers -AdGroupName $AdGroupName
    
    if ($Operation -eq "Add") {
        $missingUsers = $NTLoginIds | Where-Object { $_ -notin $updatedMembers }
        if ($missingUsers.Count -eq 0) {
            Write-Host "All users successfully added to group $AdGroupName."
        } else {
            Write-Warning "The following users were not added to group $AdGroupName: $($missingUsers -join ', ')"
        }
    } elseif ($Operation -eq "Remove") {
        $remainingUsers = $NTLoginIds | Where-Object { $_ -in $updatedMembers }
        if ($remainingUsers.Count -eq 0) {
            Write-Host "All users successfully removed from group $AdGroupName."
        } else {
            Write-Warning "The following users were not removed from group $AdGroupName: $($remainingUsers -join ', ')"
        }
    }
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

# Step 5: Verification
foreach ($groupName in $usersToAdd.Keys) {
    $users = $usersToAdd[$groupName]
    Verify-GroupMembers -NTLoginIds $users -AdGroupName $groupName -Operation "Add"
}

foreach ($groupName in $usersToRemove.Keys) {
    $users = $usersToRemove[$groupName]
    Verify-GroupMembers -NTLoginIds $users -AdGroupName $groupName -Operation "Remove"
}