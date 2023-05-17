
# Check if the IIS Admin Service is running
if ((Get-Service -Name "IISAdmin").Status -eq "Running") {
    # Restart the IIS Admin Service
    Restart-Service -Name "IISAdmin" -Force
    Write-Host "IIS Admin Service restarted."
} else {
    Write-Host "IIS Admin Service is not running."
}
