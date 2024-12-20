# Define global variable to track the current step
$Global:CurrentStep = "Unknown"

# Custom Write-Output function
function Write-Log {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message
    )
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    Write-Output "[$timestamp] [Step: $Global:CurrentStep] INFO: $Message"
}

# Custom Write-Error function
function Write-LogError {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message
    )
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    Write-Error "[$timestamp] [Step: $Global:CurrentStep] ERROR: $Message"
}