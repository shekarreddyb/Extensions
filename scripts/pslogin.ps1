param (
    [string]$ApiUrl,
    [string]$Username,
    [string]$Password
)

# Validate and set default for Username
if (-not $Username) {
    $Username = $env:USERNAME
    Write-Host "No username provided. Using the logged-in user: $Username"
}

# Prompt for Password if not provided
if (-not $Password) {
    Write-Host "Please enter your password:" -NoNewline
    $Password = Read-Host -AsSecureString | ConvertFrom-SecureString -AsPlainText
    if (-not $Password) {
        Write-Error "Password is required."
        exit 1
    }
}

# Fixed parameters
$ClientId = "openshift-challenging-client"
$RedirectUri = "$ApiUrl/oauth/token/implicit"

# Log parameters (excluding sensitive ones like password)
Write-Host "API URL: $ApiUrl"
Write-Host "Username: $Username"

# Step 1: Fetch OAuth configuration
Write-Host "Fetching OAuth configuration..."
try {
    $oauthConfigUrl = "$ApiUrl/.well-known/oauth-authorization-server"
    $oauthConfig = Invoke-RestMethod -Uri $oauthConfigUrl -Method GET
    $authorizeEndpoint = $oauthConfig.authorization_endpoint
    $tokenEndpoint = $oauthConfig.token_endpoint
    Write-Host "OAuth configuration fetched successfully."
} catch {
    Write-Error "Failed to fetch OAuth configuration: $_"
    exit 1
}

# Step 2: Call the authorize endpoint
Write-Host "Calling the authorize endpoint..."
try {
    $authorizeResponse = Invoke-RestMethod -Uri $authorizeEndpoint -Method POST -Headers @{
        Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$Username:$Password"))
    } -Body @{
        client_id       = $ClientId
        redirect_uri    = $RedirectUri
        response_type   = "code"
        code_challenge  = $CodeChallenge
        code_challenge_method = "S256"
    } -SkipHttpError -ErrorAction Stop -PassThru

    $redirectLocation = $authorizeResponse.Headers.Location
    if (-not $redirectLocation) {
        Write-Error "Authorize endpoint did not return a redirect location."
        exit 1
    }
    $code = ([System.Web.HttpUtility]::ParseQueryString(($redirectLocation -split '\?')[1])).Get("code")
    if (-not $code) {
        Write-Error "Authorization code not found in the response."
        exit 1
    }
    Write-Host "Authorization code received."
} catch {
    Write-Error "Failed to call authorize endpoint: $_"
    exit 1
}

# Step 3: Call the token endpoint
Write-Host "Calling the token endpoint..."
try {
    $tokenResponse = Invoke-RestMethod -Uri $tokenEndpoint -Method POST -Headers @{
        Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$ClientId:"))
    } -Body @{
        grant_type     = "authorization_code"
        code           = $code
        redirect_uri   = $RedirectUri
        code_verifier  = $CodeVerifier
    } -ContentType "application/x-www-form-urlencoded" -ErrorAction Stop

    $accessToken = $tokenResponse.access_token
    $expiresIn = $tokenResponse.expires_in

    if (-not $accessToken -or -not $expiresIn) {
        Write-Error "Token endpoint response does not contain access_token or expires_in."
        exit 1
    }
    Write-Host "Access token received."
} catch {
    Write-Error "Failed to call token endpoint: $_"
    exit 1
}

# Output the results
Write-Host "Access Token: $accessToken"
Write-Host "Expires In: $expiresIn seconds"