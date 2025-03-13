using System;
using System.Collections.Generic;

namespace YourAppNamespace.Models
{
    /// <summary>
    /// The "logical" database resource that the user sees at /redis/databases/{databaseId}.
    /// </summary>
    public class Database
    {
        public Guid DatabaseId { get; set; }
        public string Name { get; set; }
        public string Environment { get; set; }

        // Additional fields
        public string AppId { get; set; }
        public string Lob { get; set; }
        public string Module { get; set; }
        public string Email { get; set; }
        public string Ticket { get; set; }
        public bool CRDB { get; set; }

        // Common status fields
        public string Status { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? UpdatedTime { get; set; }

        // Navigation
        public ICollection<DatabaseDatacenter> Datacenters { get; set; }
    }

    /// <summary>
    /// Represents one physical instance of a Database in a specific DC.
    /// </summary>
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
    /// Tracks a top-level asynchronous request/transaction 
    /// for CREATE, UPDATE, DELETE, etc. on a Database resource.
    /// </summary>
    public class Request
    {
        public Guid RequestId { get; set; }
        public Guid? DatabaseId { get; set; }

        public string Operation { get; set; }
        public string Status { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Message { get; set; }
        public string RequestPayload { get; set; }

        // Navigation
        public Database Database { get; set; }
        public ICollection<RequestDetail> RequestDetails { get; set; }
    }

    /// <summary>
    /// Per-DC (or per sub-operation) details for a given Request.
    /// </summary>
    public class RequestDetail
    {
        public Guid DetailId { get; set; }
        public Guid RequestId { get; set; }

        public string DC { get; set; }
        public string Action { get; set; }
        public string Status { get; set; }

        public string Message { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string FieldChanges { get; set; }

        // Navigation
        public Request Request { get; set; }
    }
}