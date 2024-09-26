# Global variable passed as string, convert to boolean
$Force = "true"  # Replace this with the actual value passed
$Force = [bool]::Parse($Force)

# Define separate caches for AD user and group checks
$userCheckCache = @{}
$groupCheckCache = @{}

# Function to check if a user exists in AD
function Check-ADUserExists {
    param($NTLoginId)
    # Add your AD lookup logic here (e.g., using Get-ADUser)
    return $true  # Mock result, replace with actual AD check
}

# Function to check if a group exists in AD
function Check-ADGroupExists {
    param($AdGroupName)
    # Add your AD lookup logic here (e.g., using Get-ADGroup)
    return $true  # Mock result, replace with actual AD check
}

# Function to add a user to an AD group
function Add-UserToGroup {
    param($NTLoginId, $AdGroupName)
    Write-Host "Adding user $NTLoginId to group $AdGroupName"
}

# Function to remove a user from an AD group
function Remove-UserFromGroup {
    param($NTLoginId, $AdGroupName)
    Write-Host "Removing user $NTLoginId from group $AdGroupName"
}

# Sample array of objects
$items = @(
    @{ NTLoginId = 'user1'; AdGroupName = 'group1'; ApprovalStatus = 'Approved' },
    @{ NTLoginId = 'user2'; AdGroupName = 'group1'; ApprovalStatus = 'Canceled' },
    @{ NTLoginId = 'user1'; AdGroupName = 'group2'; ApprovalStatus = 'Suspended' },
    @{ NTLoginId = 'user3'; AdGroupName = 'group3'; ApprovalStatus = 'Completed' }
)

# Main loop to process each item
foreach ($item in $items) {
    $NTLoginId = $item.NTLoginId
    $AdGroupName = $item.AdGroupName
    $ApprovalStatus = $item.ApprovalStatus

    # Check if user existence is cached
    if (-not $userCheckCache.ContainsKey($NTLoginId)) {
        # If not cached, perform AD user check
        $userExists = Check-ADUserExists $NTLoginId

        # Cache the result
        $userCheckCache[$NTLoginId] = $userExists
    }

    # Check if group existence is cached
    if (-not $groupCheckCache.ContainsKey($AdGroupName)) {
        # If not cached, perform AD group check
        $groupExists = Check-ADGroupExists $AdGroupName

        # Cache the result
        $groupCheckCache[$AdGroupName] = $groupExists
    }

    # Retrieve cached results
    $userExists = $userCheckCache[$NTLoginId]
    $groupExists = $groupCheckCache[$AdGroupName]

    # Proceed only if both user and group exist in AD
    if ($userExists -and $groupExists) {
        # Determine the action based on the ApprovalStatus
        if (($ApprovalStatus -eq 'Approved' -or ($ApprovalStatus -eq 'Completed' -and $Force)) -or
            ($ApprovalStatus -eq 'Suspended' -and $Force)) {
            # Add user to group
            Add-UserToGroup -NTLoginId $NTLoginId -AdGroupName $AdGroupName
        } elseif (($ApprovalStatus -eq 'Canceled') -or ($ApprovalStatus -eq 'Suspended' -and -not $Force)) {
            # Remove user from group
            Remove-UserFromGroup -NTLoginId $NTLoginId -AdGroupName $AdGroupName
        }
    } else {
        Write-Host "Either user $NTLoginId or group $AdGroupName does not exist in AD."
    }
}