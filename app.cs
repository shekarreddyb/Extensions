public class AppInfo
{
    public string OrgName { get; set; }          // Organization name
    public string SpaceName { get; set; }        // Space name
    public string AppName { get; set; }          // Application name
    public int InstanceCount { get; set; }       // Number of instances
    public int InstanceMemoryQuota { get; set; } // Memory quota per instance (in MB or GB)
    public int InstanceDiskQuota { get; set; }   // Disk quota per instance (in MB or GB)
    public int AppMemoryQuota { get; set; }      // Total memory quota for the app (InstanceMemoryQuota * InstanceCount)
    public int AppDiskQuota { get; set; }        // Total disk quota for the app (InstanceDiskQuota * InstanceCount)
}