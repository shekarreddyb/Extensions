# Define your credentials for the remote domain
$Username = "RemoteDomain\YourUsername"
$SecurePassword = ConvertTo-SecureString "YourPassword" -AsPlainText -Force
$Credential = New-Object System.Management.Automation.PSCredential ($Username, $SecurePassword)

# Define remote domain's domain controller
$DomainController = "DC.RemoteDomain.com"

# Establish a connection to the remote domain's Active Directory
$ADSession = New-PSSession -ComputerName $DomainController -Credential $Credential
Import-PSSession $ADSession -Module ActiveDirectory

# Get the user's group memberships
$AccountName = "AccountName"
$AccountGroups = Get-ADPrincipalGroupMembership -Identity $AccountName | Select-Object Name

# Display the groups
$AccountGroups

# Close the remote session
Remove-PSSession $ADSession






#reolace file from local

# Define the local and remote file paths
$localFilePath = "C:\path\to\your\local\file"
$remoteFilePath = "C:\path\to\your\remote\file"

# Define the remote computer name
$computerName = "RemoteMachineName"

# Create a new PSSession
$session = New-PSSession -ComputerName $computerName

# Invoke command on the remote machine
Invoke-Command -Session $session -ScriptBlock {
    param($remoteFilePath, $localFileContent)

    # Ensure the remote directory exists
    $remoteDirectory = Split-Path -Path $remoteFilePath -Parent
    if (-not (Test-Path -Path $remoteDirectory)) {
        New-Item -ItemType Directory -Force -Path $remoteDirectory
    }

    # Write the local file content to the remote file
    [System.IO.File]::WriteAllBytes($remoteFilePath, $localFileContent)

} -ArgumentList $remoteFilePath, (Get-Content -Path $localFilePath -Encoding Byte)

# Close the PSSession
Remove-PSSession -Session $session

