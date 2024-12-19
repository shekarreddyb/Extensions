#!/bin/bash

# Input parameters
ApiUrl=""
Username=""
Password=""
ClientId="openshift-challenging-client"

# Prompt for API URL if not provided
if [ -z "$ApiUrl" ]; then
  read -p "Enter the API URL: " ApiUrl
fi

# Use the logged-in username if not provided
if [ -z "$Username" ]; then
  Username=$(whoami)
  echo "No username provided. Using the logged-in user: $Username"
fi

# Prompt for Password if not provided
if [ -z "$Password" ]; then
  read -sp "Please enter your password: " Password
  echo
fi

# Redirect URI
RedirectUri="${ApiUrl}/oauth/token/implicit"

# Log parameters (excluding sensitive ones like password)
echo "API URL: $ApiUrl"
echo "Username: $Username"

# Step 1: Fetch OAuth configuration
echo "Fetching OAuth configuration..."
oauthConfig=$(curl -s "${ApiUrl}/.well-known/oauth-authorization-server")

authorizeEndpoint=$(echo "$oauthConfig" | grep -o '"authorization_endpoint":"[^"]*' | cut -d'"' -f4)
tokenEndpoint=$(echo "$oauthConfig" | grep -o '"token_endpoint":"[^"]*' | cut -d'"' -f4)

if [ -z "$authorizeEndpoint" ] || [ -z "$tokenEndpoint" ]; then
  echo "Failed to fetch OAuth configuration." >&2
  exit 1
fi
echo "OAuth configuration fetched successfully."

# Generate code_verifier and code_challenge
codeVerifier=$(openssl rand -base64 32 | tr -d '\n' | tr '/+' '_-' | tr -d '=')
codeChallenge=$(echo -n "$codeVerifier" | openssl dgst -sha256 -binary | base64 | tr -d '\n' | tr '/+' '_-' | tr -d '=')

# Step 2: Call the authorize endpoint
echo "Calling the authorize endpoint..."
authResponse=$(curl -s -i -X POST "$authorizeEndpoint" \
  -H "Authorization: Basic $(echo -n "${Username}:${Password}" | base64)" \
  -d "client_id=${ClientId}&redirect_uri=${RedirectUri}&response_type=code&code_challenge=${codeChallenge}&code_challenge_method=S256")

redirectLocation=$(echo "$authResponse" | grep -i "Location:" | awk '{print $2}' | tr -d '\r')
code=$(echo "$redirectLocation" | grep -o 'code=[^&]*' | cut -d '=' -f 2)

if [ -z "$code" ]; then
  echo "Authorization code not found." >&2
  exit 1
fi
echo "Authorization code received."

# Step 3: Call the token endpoint
echo "Calling the token endpoint..."
tokenResponse=$(curl -s -X POST "$tokenEndpoint" \
  -H "Authorization: Basic $(echo -n "${ClientId}:" | base64)" \
  -d "grant_type=authorization_code&code=${code}&redirect_uri=${RedirectUri}&code_verifier=${codeVerifier}" \
  -H "Content-Type: application/x-www-form-urlencoded")

accessToken=$(echo "$tokenResponse" | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)
expiresIn=$(echo "$tokenResponse" | grep -o '"expires_in":[0-9]*' | cut -d':' -f2)

if [ -z "$accessToken" ] || [ -z "$expiresIn" ]; then
  echo "Failed to retrieve access token." >&2
  exit 1
fi
echo "Access token received."

# Output the results
echo "Access Token: $accessToken"
echo "Expires In: $expiresIn seconds"