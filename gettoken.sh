#!/bin/bash

# Make the HTTP call and store the response headers
response_headers=$(curl -s -D - -o /dev/null "https://your-url-here.com")

# Extract the 'Location' header
location_header=$(echo "$response_headers" | grep -i "^Location:" | awk '{print $2}' | tr -d '\r')

# Check if the Location header is present
if [[ -n $location_header ]]; then
    echo "Location header found: $location_header"

    # Extract the access_token from the URL fragment
    access_token=$(echo "$location_header" | grep -oP '(?<=#access_token=)[^&]+')

    if [[ -n $access_token ]]; then
        echo "Access token found: $access_token"
    else
        echo "Access token not found in Location header."
    fi
else
    echo "Location header not found in response."
fi