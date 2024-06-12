# Define the SQL Server and database
$ServerName = "YourServerName"
$DatabaseName = "YourDatabaseName"

# Define the SQL query
$Query = "SELECT * FROM YourTable"

# Execute the SQL query
$Result = Invoke-Sqlcmd -ServerInstance $ServerName -Database $DatabaseName -Credential $SqlCredential -Query $Query

# Output the result
$Result