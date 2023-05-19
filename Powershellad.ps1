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

#download file from remote

# Define the remote and local file paths
$remoteFilePath = "C:\path\to\your\remote\file"
$localFilePath = "C:\path\to\your\local\file"

# Define the remote computer name
$computerName = "RemoteMachineName"

# Create a new PSSession
$session = New-PSSession -ComputerName $computerName

# Invoke command on the remote machine to get the file content
$fileContent = Invoke-Command -Session $session -ScriptBlock {
    param($remoteFilePath)

    # Get the content of the remote file
    [System.IO.File]::ReadAllBytes($remoteFilePath)

} -ArgumentList $remoteFilePath

# Write the remote file content to the local file
[System.IO.File]::WriteAllBytes($localFilePath, $fileContent)

# Close the PSSession
Remove-PSSession -Session $session



# chnage kernel mode

# Import the WebAdministration Module
Import-Module WebAdministration

# Set the website name
$websiteName = "YourWebsiteName"

# Get the windowsAuthentication configuration section
$winAuthSection = Get-WebConfigurationSection -pspath "MACHINE/WEBROOT/APPHOST/$websiteName" -filter "system.webServer/security/authentication/windowsAuthentication"

# Set useKernelMode to false
$winAuthSection["useKernelMode"] = $false

# Apply the changes
$winAuthSection | Set-WebConfiguration -pspath "MACHINE/WEBROOT/APPHOST/$websiteName" -filter "system.webServer/security/authentication/windowsAuthentication"


