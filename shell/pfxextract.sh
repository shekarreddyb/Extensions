#!/bin/bash

# Input: PFX file name
PFX_FILE=$1

# Output files
CERT_FILE="client.crt"
KEY_FILE="client.key"
CA_FILE="ca_bundle.crt"

if [ -z "$PFX_FILE" ]; then
  echo "Usage: $0 <pfx-file>"
  exit 1
fi

# Step 1: Extract private key with password (temporarily)
openssl pkcs12 -in "$PFX_FILE" -nocerts -out temp.key

# Step 2: Remove passphrase from private key
openssl rsa -in temp.key -out "$KEY_FILE"
rm temp.key

# Step 3: Extract client certificate
openssl pkcs12 -in "$PFX_FILE" -clcerts -nokeys -out "$CERT_FILE"

# Step 4: Extract CA certificates (if any)
openssl pkcs12 -in "$PFX_FILE" -cacerts -nokeys -chain -out "$CA_FILE"

echo "Done!"
echo "Generated files:"
echo " - $CERT_FILE (client cert)"
echo " - $KEY_FILE (private key)"
echo " - $CA_FILE (CA bundle cert)"