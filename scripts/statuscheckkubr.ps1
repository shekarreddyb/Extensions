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
function Get-JobDetails {
    $response = curl.exe -s -X GET `
        -H "Authorization: Bearer $($token)" `
        -H "Content-Type: application/json" `
        "$apiServer/apis/batch/v1/namespaces/$namespace/jobs/$jobName"

    # Convert response JSON to PowerShell object
    return $response | ConvertFrom-Json
}

# Function to check job status
function Check-JobStatus {
    param($job)

    $status = $job.status

    # Check conditions
    if ($status.conditions) {
        foreach ($condition in $status.conditions) {
            if ($condition.type -eq "Complete" -and $condition.status -eq "True") {
                Write-Output "Job has completed successfully."
                return "Complete"
            }
            if ($condition.type -eq "Failed" -and $condition.status -eq "True") {
                Write-Output "Job has failed."
                return "Failed"
            }
        }
    }

    # Check if the job is still running
    if ($status.active -gt 0) {
        Write-Output "Job is still running..."
        return "Running"
    }

    # Default: Unknown state
    Write-Output "Unknown job status. Investigate manually."
    return "Unknown"
}

# Poll the job status until completion, failure, or timeout
while ($true) {
    # Get the current job details
    $job = Get-JobDetails

    # Check the job completion status
    $status = Check-JobStatus -job $job
    if ($status -ne "Running") {
        break
    }

    # Check timeout
    if ((Get-Date) -gt $startTime.AddSeconds($timeoutSeconds)) {
        Write-Output "Timeout reached. Stopping status checks."
        $status = "Timeout"
        break
    }

    # Wait before polling again
    Start-Sleep -Seconds $pollIntervalSeconds
}

# Output the final status
Write-Output "Final Status: $status"