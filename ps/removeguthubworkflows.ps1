# CONFIGURE THESE VALUES
$token = "ghp_your_token_here"  # GitHub PAT with repo access
$owner = "your-github-username"
$repo = "your-repo-name"

# Set headers for GitHub API
$headers = @{
    Authorization = "token $token"
    Accept        = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

# Get all workflow runs (you may need to paginate if there are many)
$allRunsUrl = "https://api.github.com/repos/$owner/$repo/actions/runs?per_page=100"
$response = Invoke-RestMethod -Uri $allRunsUrl -Headers $headers -Method Get

# Get all run IDs, sorted by most recent first
$allRuns = $response.workflow_runs | Sort-Object created_at -Descending
$runsToDelete = $allRuns | Select-Object -Skip 5

Write-Host "Found $($allRuns.Count) workflow runs. Deleting $($runsToDelete.Count)..."

foreach ($run in $runsToDelete) {
    $deleteUrl = "https://api.github.com/repos/$owner/$repo/actions/runs/$($run.id)"
    Write-Host "Deleting run ID: $($run.id) - $($run.created_at)"
    Invoke-RestMethod -Uri $deleteUrl -Headers $headers -Method Delete
}