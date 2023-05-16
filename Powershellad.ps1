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
