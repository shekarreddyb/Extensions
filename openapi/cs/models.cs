using System;
using System.Collections.Generic;

namespace YourAppNamespace.Models
{
    public class Database
    {
        public Guid DatabaseId { get; set; }
        public string Name { get; set; }
        public string Environment { get; set; }

        public string AppId { get; set; }
        public string Lob { get; set; }
        public string Module { get; set; }
        public string Email { get; set; }
        public string Ticket { get; set; }
        public bool CRDB { get; set; }

        public string Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? UpdatedTime { get; set; }

        // Navigation: one Database -> many DatabaseDatacenters
        public ICollection<DatabaseDatacenter> Datacenters { get; set; }
    }

    public class DatabaseDatacenter
    {
        public Guid DatacenterId { get; set; }
        public Guid DatabaseId { get; set; }

        public string DC { get; set; }
        public string Size { get; set; }
        public bool Replication { get; set; }

        public string Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? UpdatedTime { get; set; }

        // Navigation
        public Database Database { get; set; }
    }

    /// <summary>
    /// The top-level transaction/async request (like "CREATE"/"UPDATE"/"DELETE" on a Database).
    /// </summary>
    public class Request
    {
        public Guid RequestId { get; set; }
        public Guid? DatabaseId { get; set; }

        public string Operation { get; set; }  // e.g. CREATE, UPDATE, DELETE
        public string Status { get; set; }     // PENDING, COMPLETED, etc.
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Message { get; set; }
        public string RequestPayload { get; set; }

        // Navigation
        public Database Database { get; set; }
        public ICollection<RequestDetail> RequestDetails { get; set; }
    }

    /// <summary>
    /// Each sub-operation in a Request. e.g. a DC-level create or update.
    /// </summary>
    public class RequestDetail
    {
        public Guid DetailId { get; set; }
        public Guid RequestId { get; set; }

        public string DC { get; set; }      // e.g. "EastDC"
        public string Action { get; set; }  // CREATE/UPDATE/DELETE
        public string Status { get; set; }  // PENDING/SUCCESS/FAILED
        public string Message { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string FieldChanges { get; set; }

        // Navigation
        public Request Request { get; set; }
    }
}