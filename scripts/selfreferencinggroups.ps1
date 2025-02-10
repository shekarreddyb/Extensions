$cldFilter = "YourCLD*"  # Replace with your specific CLD filter
$outputFile = "C:\Temp\SelfMemberGroups.txt"  # Change path as needed

$groups = Get-ADGroup -Filter "Name -like '$cldFilter'" | Select-Object -ExpandProperty Name

$selfMemberGroups = @()

foreach ($group in $groups) {
    $members = Get-ADGroupMember -Identity $group -ErrorAction SilentlyContinue
    if ($members -and $members.Name -contains $group) {
        $selfMemberGroups += $group
    }
}

# Save to a file, each group on a new line
$selfMemberGroups | Out-File -FilePath $outputFile -Encoding UTF8

Write-Output "Groups saved to $outputFile"