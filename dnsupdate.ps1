# Define the force variable
$force = $false

# Define DNS record objects for CNAME operations with additional fields
$dnsRecords = @(
    [PSCustomObject]@{AppDnsEntryId=[guid]::NewGuid(); AppRequestId=[guid]::NewGuid(); VanityUrl='example.com'; Destination='host1.example.com'; DnsRecordType='CNAME'; DnsZone='zone1.com'; IsRoundRobin=$false; DnsOperation='Add'; DnsAprovalStatus='Approved'; DnsApprovalValue=1; TimeToLive=300; Forced=$false},
    [PSCustomObject]@{AppDnsEntryId=[guid]::NewGuid(); AppRequestId=[guid]::NewGuid(); VanityUrl='delete.me.com'; Destination=$null; DnsRecordType='CNAME'; DnsZone='zone1.com'; IsRoundRobin=$false; DnsOperation='Delete'; DnsAprovalStatus='Cancelled'; DnsApprovalValue=1; TimeToLive=300; Forced=$false},
    [PSCustomObject]@{AppDnsEntryId=[guid]::NewGuid(); AppRequestId=[guid]::NewGuid(); VanityUrl='modify.me.com'; Destination='newhost.example.com'; DnsRecordType='CNAME'; DnsZone='zone2.com'; IsRoundRobin=$false; DnsOperation='Modify'; DnsAprovalStatus='Approved'; DnsApprovalValue=1; TimeToLive=300; Forced=$false}
)

# List of DNS servers
$dnsServers = @('dns1.example.com', 'dns2.example.com')

# Function to perform DNS pre-check
function Check-DNSRecordExists {
    param ($record, $server)
    try {
        $result = Resolve-DnsName -Name $record.VanityUrl -Type $record.DnsRecordType -Server $server -DnsOnly -ErrorAction Stop
        return $result
    } catch {
        return $null
    }
}

# Function to verify DNS changes on a single server
function Verify-DNSChange {
    param ($record, $server)
    $result = Check-DNSRecordExists -record $record -server $server
    if ($record.DnsOperation -eq 'Add' -or $record.DnsOperation -eq 'Modify') {
        return $result -and $result.NameHost -eq $record.Destination
    } elseif ($record.DnsOperation -eq 'Delete') {
        return -not $result
    }
}

# Conditional filtering based on the force variable
$approvedRecords = $dnsRecords | Where-Object {
    if ($force) {
        ($_.DnsOperation -in @('Add', 'Modify') -and $_.DnsAprovalStatus -eq 'Approved') -or 
        ($_.DnsOperation -eq 'Delete' -and $_.DnsAprovalStatus -in @('Approved', 'Cancelled')) -or
        ($_.DnsAprovalStatus -eq 'Completed' -and ($_.DnsOperation -in @('Add', 'Modify', 'Delete')))
    } else {
        ($_.DnsOperation -in @('Add', 'Modify') -and $_.DnsAprovalStatus -eq 'Approved') -or 
        ($_.DnsOperation -eq 'Delete' -and $_.DnsAprovalStatus -in @('Approved', 'Cancelled'))
    }
}

# Mark records as forced if applicable
if ($force) {
    $approvedRecords | ForEach-Object {
        if ($_.DnsAprovalStatus -eq 'Completed') {
            $_.Forced = $true
        }
    }
}

# Perform pre-checks in parallel
$preCheckResults = $dnsServers | ForEach-Object -Parallel {
    param ($server, $approvedRecords)
    $results = $approvedRecords | ForEach-Object -Parallel {
        param ($record, $server)
        $result = Check-DNSRecordExists -record $record -server $server
        $exists = $result -ne $null
        $isDifferentDestination = $exists -and $result.NameHost -ne $record.Destination

        $shouldProcess = switch ($record.DnsOperation) {
            'Add' { -not $exists }
            'Modify' { $exists -and $isDifferentDestination }
            'Delete' { $exists }
        }

        [PSCustomObject]@{
            Server = $server
            Record = $record
            ShouldProcess = $shouldProcess
        }
    } -ArgumentList $_, $using:approvedRecords

    $results
} -ArgumentList $approvedRecords -ThrottleLimit 5

# Group pre-check results by server
$groupedResults = $preCheckResults | Group-Object -Property Server

# Process the results in parallel for each server
$updateResults = $groupedResults | ForEach-Object -Parallel {
    param ($group, $approvedRecords)
    $server = $group.Name
    $recordsToProcess = $group.Group | Where-Object { $_.ShouldProcess }

    if ($recordsToProcess.Count -eq 0) {
        Write-Host "No operations to process for $server"
        return [PSCustomObject]@{
            Server = $server
            Success = $true
            Message = "No operations to process"
        }
    }

    # Group records by zone
    $zones = $recordsToProcess | Group-Object -Property Record.DnsZone

    # Generate nsupdate content for each server and zone
    $nsupdateContent = "server $server`n"
    foreach ($zoneGroup in $zones) {
        $zone = $zoneGroup.Name
        $nsupdateContent += "zone $zone`n"
        foreach ($result in $zoneGroup.Group) {
            $ttl = $result.Record.TimeToLive
            switch ($result.Record.DnsOperation) {
                'Add' {
                    $nsupdateContent += "update add $($result.Record.VanityUrl) $ttl $($result.Record.DnsRecordType) $($result.Record.Destination)`n"
                }
                'Delete' {
                    $nsupdateContent += "update delete $($result.Record.VanityUrl) $($result.Record.DnsRecordType)`n"
                }
                'Modify' {
                    $nsupdateContent += "update delete $($result.Record.VanityUrl) $($result.Record.DnsRecordType)`n"
                    $nsupdateContent += "update add $($result.Record.VanityUrl) $ttl $($result.Record.DnsRecordType) $($result.Record.Destination)`n"
                }
            }
        }
        $nsupdateContent += "send`n"
    }

    # Save to file and execute nsupdate commands
    $filePath = "nsupdate_commands_$server.txt"
    $nsupdateContent | Out-File -FilePath $filePath -Encoding ASCII
    Write-Host "Commands for $server written to $filePath"

    # Execute the nsupdate command
    & nsupdate $filePath
    $updateSuccess = $?
    if (-not $updateSuccess){
        Write-Host "Failed to execute nsupdate commands for $server."
        return [PSCustomObject]@{
            Server = $server
            Success = $false
            Message = "Failed to execute nsupdate commands"
        }
    } else {
        Write-Host "Successfully updated DNS records on $server."
        return [PSCustomObject]@{
            Server = $server
            Success = $true
            Message = "Successfully updated DNS records"
        }
    }
} -ArgumentList $approvedRecords -ThrottleLimit 5

# Wait for all parallel operations to complete
$updateResults = $updateResults | Where-Object { $_ }

# Verify the changes on all servers in parallel
$verificationResults = $approvedRecords | ForEach-Object -Parallel {
    param ($record, $dnsServers)
    $successfulServers = $dnsServers | ForEach-Object -Parallel {
        param ($server, $record)
        [PSCustomObject]@{
            Record = $record
            Server = $server
            Success = Verify-DNSChange -record $record -server $server
        }
    } -ArgumentList $_, $record

    $successfulServers = $successfulServers | Where-Object { $_.Success } | Select-Object -ExpandProperty Server

    if ($successfulServers.Count -eq $dnsServers.Count) {
        Write-Host "DNS change for $($record.VanityUrl) verified on all servers. Updating database for AppDnsEntryId $($record.AppDnsEntryId)"
        # Update the database here
    } elseif ($successfulServers.Count -gt 0) {
        Write-Warning "DNS change for $($record.VanityUrl) verified on some servers: $($successfulServers -join ', ')."
    } else {
        Write-Host "DNS change for $($record.VanityUrl) not verified on any servers."
    }
} -ArgumentList $dnsServers -ThrottleLimit 5
