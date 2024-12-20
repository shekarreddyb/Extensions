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

# Function to check job completion status
function Check-JobCompletion {
    param($job)

    $spec = $job.spec
    $status = $job.status

    # Get values from spec and status
    $requiredCompletions = $spec.completions
    $parallelism = $spec.parallelism
    $backoffLimit = $spec.backoffLimit
    $succeeded = $status.succeeded
    $failed = $status.failed
    $active = $status.active

    # Check if the job is complete
    if ($succeeded -ge $requiredCompletions) {
        Write-Output "Job has completed successfully."
        return "Complete"
    }

    # Check if the job has failed due to exceeding the backoff limit
    if ($failed -ge $backoffLimit) {
        Write-Output "Job has failed due to exceeded retries."
        return "Failed"
    }

    # Check if the job is still running
    if ($active -gt 0) {
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
    $status = Check-JobCompletion -job $job
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