# Variables
$namespace = "<namespace>"         # Replace with your job's namespace
$jobName = "<job-name>"            # Replace with your job's name
$apiServer = "https://<k8s-api-server>" # Replace with your Kubernetes API server
$tokenPath = "/var/run/secrets/kubernetes.io/serviceaccount/token"
$timeoutSeconds = 300              # Timeout in seconds
$pollIntervalSeconds = 10          # Time between status checks

# Read the Kubernetes API token
$token = Get-Content $tokenPath

# Start time for timeout logic
$startTime = Get-Date

# Function to call Kubernetes API and parse the response
function Get-JobStatus {
    $response = curl.exe -s -X GET `
        -H "Authorization: Bearer $($token)" `
        -H "Content-Type: application/json" `
        "$apiServer/apis/batch/v1/namespaces/$namespace/jobs/$jobName"

    # Convert response JSON to PowerShell object
    return $response | ConvertFrom-Json
}

# Function to check job status
function Check-JobStatus {
    param($status)

    # Check if the job is completed
    if ($status.conditions -and ($status.conditions | Where-Object { $_.type -eq "Complete" -and $_.status -eq "True" })) {
        Write-Output "Job has completed successfully."
        return $true
    }

    # Check if the job has failed
    if ($status.conditions -and ($status.conditions | Where-Object { $_.type -eq "Failed" -and $_.status -eq "True" })) {
        Write-Output "Job has failed."
        return $true
    }

    # Check if the job is still running
    if ($status.active -gt 0) {
        Write-Output "Job is still running..."
        return $false
    }

    # Default: Job is not active and no complete/failed condition found
    Write-Output "Unknown job status."
    return $true
}

# Poll the job status until completion, failure, or timeout
while ($true) {
    # Get the current job status
    $job = Get-JobStatus

    # Check the job status
    if (Check-JobStatus -status $job.status) {
        break
    }

    # Check timeout
    if ((Get-Date) -gt $startTime.AddSeconds($timeoutSeconds)) {
        Write-Output "Timeout reached. Stopping status checks."
        break
    }

    # Wait before polling again
    Start-Sleep -Seconds $pollIntervalSeconds
}

Write-Output "Status check completed."