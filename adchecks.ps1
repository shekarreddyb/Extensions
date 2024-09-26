# Global variable passed as string, convert to boolean
$Force = "true"  # Replace this with the actual value passed
$Force = [bool]::Parse($Force)

# Define separate caches for AD user, group checks, and group members
$userCheckCache = @{}
$groupCheckCache = @{}
$groupMembersCache = @{}

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
    Write-Host "Adding users: $($NTLoginIds -join ', ') to group $AdGroupName"
}

# Function to remove multiple users from an AD group
function Remove-UsersFromGroup {
    param($NTLoginIds, $AdGroupName)
    Write-Host "Removing users: $($NTLoginIds -join ', ') from group $AdGroupName"
}

# Function to log specific actions
function Log-Action {
    param($NTLoginIds, $AdGroupName, $ApprovalStatus, $Action)
    Write-Host "Logging: $Action for users $($NTLoginIds -join ', ') in group $AdGroupName with status $ApprovalStatus."
}

# Sample array of objects
$items = @(
    @{ NTLoginId = 'user1'; AdGroupName = 'group1'; ApprovalStatus = 'Approved' },
    @{ NTLoginId = 'user2'; AdGroupName = 'group1'; ApprovalStatus = 'Canceled' },
    @{ NTLoginId = 'user3'; AdGroupName = 'group2'; ApprovalStatus = 'Suspended' },
    @{ NTLoginId = 'user4'; AdGroupName = 'group2'; ApprovalStatus = 'Completed' }
)

# Create collections to hold grouped users for add and remove operations
$usersToAdd = @{}
$usersToRemove = @{}

# Main loop to process each item
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
            $groupMembersCache[$AdGroupName] = Get-ADGroupMembers $AdGroupName
        }
    }

    # Retrieve cached results
    $userExists = $userCheckCache[$NTLoginId]
    $groupExists = $groupCheckCache[$AdGroupName]
    $groupMembers = $groupMembersCache[$AdGroupName]

    # Only proceed if both the user and group exist
    if ($userExists -and $groupExists) {
        if (($ApprovalStatus -eq 'Approved' -or ($ApprovalStatus -eq 'Completed' -and $Force)) -or
            ($ApprovalStatus -eq 'Suspended' -and $Force)) {
            # Check if the user is already in the group
            if ($groupMembers -contains $NTLoginId) {
                Write-Host "User $NTLoginId is already a member of group $AdGroupName. Skipping addition."
            } else {
                # Add to usersToAdd collection
                if (-not $usersToAdd.ContainsKey($AdGroupName)) {
                    $usersToAdd[$AdGroupName] = @()
                }
                $usersToAdd[$AdGroupName] += $NTLoginId
            }
        } elseif (($ApprovalStatus -eq 'Canceled') -or ($ApprovalStatus -eq 'Suspended' -and -not $Force)) {
            # Check if the user is in the group before attempting to remove
            if ($groupMembers -notcontains $NTLoginId) {
                Write-Host "User $NTLoginId is not a member of group $AdGroupName. Skipping removal."
            } else {
                # Add to usersToRemove collection
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

# Perform add operations
foreach ($groupName in $usersToAdd.Keys) {
    $users = $usersToAdd[$groupName]
    if ($users.Count -gt 0) {
        # Log the addition and perform the add operation
        Log-Action -NTLoginIds $users -AdGroupName $groupName -ApprovalStatus "Approved/Completed" -Action "Adding"
        Add-UsersToGroup -NTLoginIds $users -AdGroupName $groupName
    }
}

# Perform remove operations
foreach ($groupName in $usersToRemove.Keys) {
    $users = $usersToRemove[$groupName]
    if ($users.Count -gt 0) {
        # Log the removal and perform the remove operation
        Log-Action -NTLoginIds $users -AdGroupName $groupName -ApprovalStatus "Canceled/Suspended" -Action "Removing"
        Remove-UsersFromGroup -NTLoginIds $users -AdGroupName $groupName
    }
}