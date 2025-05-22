#!/bin/bash

# Start with an empty JSON object
json='{}'

# Conditionally add keys
if [ "$ADD_NAME" == "true" ]; then
    json=$(echo "$json" | jq '. + {name: "John"}')
fi

if [ "$ADD_AGE" == "true" ]; then
    json=$(echo "$json" | jq '. + {age: 30}')
fi

if [ "$ADD_ACTIVE" == "true" ]; then
    json=$(echo "$json" | jq '. + {active: true}')
fi

# Use curl to POST the JSON
curl -X POST https://example.com/api \
     -H "Content-Type: application/json" \
     -d "$json"