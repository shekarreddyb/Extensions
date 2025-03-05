// =====================
// Domain Layer
// =====================

namespace Domain.Entities
{
    // Represents a cluster in a datacenter.
    public class Cluster
    {
        public string ClusterId { get; set; }
        public string ClusterName { get; set; }
        public string Datacenter { get; set; }
        public string Environment { get; set; }
        public int AvailableCapacity { get; set; }
        // For simplicity, assume each cluster knows its base URI.
        public string BaseUri { get; set; }
    }
}

namespace Domain.Interfaces
{
    using Domain.Entities;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    // Abstraction to query clusters and database names.
    public interface IClusterRepository
    {
        Task<IEnumerable<Cluster>> GetClustersByDatacenterAndEnvironmentAsync(string datacenter, string environment);
        Task<IEnumerable<string>> GetDatabaseNamesByClusterAsync(string clusterId, string appId, string lob, string environment, string datacenter);
    }

    // Abstraction to provision a database on a cluster.
    public interface IDatabaseCreator
    {
        Task<BdbResponse> CreateBdbAsync(string baseUri, object payload);
        Task<CrdbResponse> CreateCrdbAsync(string baseUri, object payload);
    }
}

namespace Domain.Models
{
    // Domain model to represent the result of a provisioning request.
    public class DatabaseProvisioningResult
    {
        public string TransactionId { get; set; }
        public string DatabaseUid { get; set; }
        public string DatabaseName { get; set; }
        public string ClusterName { get; set; }
        public string? CrdbGuid { get; set; }
    }
}

namespace Domain.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    // Domain service that encapsulates the business rules.
    public class DatabaseProvisioningDomainService
    {
        private readonly IClusterRepository _clusterRepository;
        private readonly IDatabaseCreator _databaseCreator;

        public DatabaseProvisioningDomainService(IClusterRepository clusterRepository, IDatabaseCreator databaseCreator)
        {
            _clusterRepository = clusterRepository;
            _databaseCreator = databaseCreator;
        }

        // Core method: given a command, select clusters, form DB names, call Redis APIs, and return results.
        public async Task<Result<List<DatabaseProvisioningResult>, List<string>>> ProvisionDatabasesAsync(Application.Commands.ProvisionDatabaseCommand command)
        {
            List<DatabaseProvisioningResult> results = new List<DatabaseProvisioningResult>();
            List<string> errors = new List<string>();

            // Process each datacenter provided in the command.
            foreach (var dc in command.DatacentersList)
            {
                // 1. Get clusters in this datacenter for the given environment.
                var clusters = await _clusterRepository.GetClustersByDatacenterAndEnvironmentAsync(dc, command.Environment);
                if (!clusters.Any())
                {
                    errors.Add($"No clusters found in datacenter {dc} for environment {command.Environment}.");
                    continue;
                }

                // 2. Pick the best cluster based on available capacity.
                // (Also, if the app already has a DB in one of these clusters, you might select that one.)
                var bestCluster = clusters.OrderByDescending(c => c.AvailableCapacity).First();

                // 3. Form the base database name.
                string baseDbName = $"{command.AppId}-{command.Lob}-{command.Environment}-{dc}";
                // 4. Query existing database names to determine sequence number.
                var existingNames = await _clusterRepository.GetDatabaseNamesByClusterAsync(bestCluster.ClusterId, command.AppId, command.Lob, command.Environment, dc);
                int seq = 1;
                if (existingNames.Any())
                {
                    // Assume names are in the format: baseName-seq.
                    seq = existingNames.Select(n =>
                    {
                        var parts = n.Split('-');
                        return int.TryParse(parts.Last(), out int number) ? number : 0;
                    }).Max() + 1;
                }
                string dbName = $"{baseDbName}-{seq}";

                // 5. Call Redis API based on iscrdb flag.
                if (!command.IsCrdb)
                {
                    // Prepare payload for regular database creation (v1/bdbs).
                    var bdbPayload = new
                    {
                        name = dbName,
                        memory_size = command.MemorySize,
                        port = 6379, // Example port.
                        replication = true,
                        aof_persistence = false,
                        tls_enabled = true,
                        module = command.Module  // Only applicable when not CRDB.
                    };

                    // Call the /v1/bdbs endpoint.
                    try
                    {
                        var bdbResponse = await _databaseCreator.CreateBdbAsync(bestCluster.BaseUri, bdbPayload);
                        results.Add(new DatabaseProvisioningResult
                        {
                            TransactionId = bdbResponse.transaction_id,
                            DatabaseUid = bdbResponse.uid,
                            DatabaseName = bdbResponse.name,
                            ClusterName = bestCluster.ClusterName
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error creating BDB in datacenter {dc}: {ex.Message}");
                    }
                }
                else
                {
                    // CRDB case:
                    // Gather all clusters in the datacenter to include as instances.
                    var clustersInDc = clusters.ToList();
                    var instanceClusterIds = clustersInDc.Select(c => c.ClusterId).ToList();
                    var crdbPayload = new
                    {
                        name = dbName,
                        regions = instanceClusterIds,
                        port = 6380, // Example port for CRDB.
                        replication = true
                    };

                    // Call the /v1/crdbs endpoint.
                    try
                    {
                        var crdbResponse = await _databaseCreator.CreateCrdbAsync(bestCluster.BaseUri, crdbPayload);
                        results.Add(new DatabaseProvisioningResult
                        {
                            TransactionId = crdbResponse.transaction_id,
                            DatabaseUid = crdbResponse.uid,
                            DatabaseName = crdbResponse.name,
                            ClusterName = bestCluster.ClusterName,
                            CrdbGuid = crdbResponse.crdb_guid
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error creating CRDB in datacenter {dc}: {ex.Message}");
                    }
                }
            }

            if (errors.Any())
                return Result<List<DatabaseProvisioningResult>, List<string>>.Fail(errors);

            return Result<List<DatabaseProvisioningResult>, List<string>>.Ok(results);
        }
    }
}

namespace Domain // Simple Result monad
{
    public class Result<TSuccess, TError>
    {
        public TSuccess? Success { get; }
        public TError? Error { get; }
        public bool IsSuccess { get; }
        private Result(TSuccess success) { Success = success; IsSuccess = true; }
        private Result(TError error) { Error = error; IsSuccess = false; }
        public static Result<TSuccess, TError> Ok(TSuccess success) => new(success);
        public static Result<TSuccess, TError> Fail(TError error) => new(error);
    }
}

// =====================
// Application Layer
// =====================

namespace Application.Commands
{
    using MediatR;
    using System.Collections.Generic;

    // This combined command acts as both the API input DTO and the MediatR command.
    public class ProvisionDatabaseCommand : IRequest<Result<ProvisionDatabaseResponse, List<string>>>
    {
        public string AppId { get; set; }
        public string Lob { get; set; }
        public string Environment { get; set; }
        public List<string> DatacentersList { get; set; }
        public bool IsCrdb { get; set; }
        public string Module { get; set; }  // Used only if IsCrdb is false.
        public int MemorySize { get; set; }
        public TlsSettings TlsSettings { get; set; }
    }

    public class TlsSettings
    {
        public string OU { get; set; }
        public string CN { get; set; }
    }

    // Response DTO returned from the application.
    public class ProvisionDatabaseResponse
    {
        public string AppId { get; set; }
        public string Lob { get; set; }
        public System.DateTime CreatedOn { get; set; }
        public string TransactionId { get; set; }
        public string DbType { get; set; }  // "BDB" or "CRDB"
        public string ClusterName { get; set; }
        public string DbName { get; set; }
        public bool IsCrdb { get; set; }
        public string DatabaseUid { get; set; }
        public string? CrdbGuid { get; set; }
    }
}

namespace Application.Handlers
{
    using MediatR;
    using Domain.Services;
    using Application.Commands;
    using Domain.Models;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    // This MediatR handler orchestrates the command by calling into the domain service.
    public class ProvisionDatabaseCommandHandler : IRequestHandler<ProvisionDatabaseCommand, Result<ProvisionDatabaseResponse, List<string>>>
    {
        private readonly DatabaseProvisioningDomainService _domainService;
        public ProvisionDatabaseCommandHandler(DatabaseProvisioningDomainService domainService)
        {
            _domainService = domainService;
        }
        public async Task<Result<ProvisionDatabaseResponse, List<string>>> Handle(ProvisionDatabaseCommand command, CancellationToken cancellationToken)
        {
            // Call the domain service.
            var domainResult = await _domainService.ProvisionDatabasesAsync(command);

            // Map the domain result(s) into a response DTO.
            if (domainResult.IsSuccess)
            {
                // For simplicity, assume one result per datacenter; here we join them into one response.
                // In a real-world scenario, you might return a list or a more complex object.
                var first = domainResult.Success.First();
                var response = new Application.Commands.ProvisionDatabaseResponse
                {
                    AppId = command.AppId,
                    Lob = command.Lob,
                    CreatedOn = System.DateTime.UtcNow,
                    TransactionId = first.TransactionId,
                    DbType = command.IsCrdb ? "CRDB" : "BDB",
                    ClusterName = first.ClusterName,
                    DbName = first.DatabaseName,
                    IsCrdb = command.IsCrdb,
                    DatabaseUid = first.DatabaseUid,
                    CrdbGuid = first.CrdbGuid
                };
                return Domain.Result<ProvisionDatabaseResponse, List<string>>.Ok(response);
            }
            return Domain.Result<ProvisionDatabaseResponse, List<string>>.Fail(domainResult.Error);
        }
    }
}

// =====================
// Infrastructure Layer
// =====================

namespace Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    // A sample/fake implementation of IClusterRepository.
    public class ClusterRepository : IClusterRepository
    {
        // In a real implementation, use HttpClient or a database.
        public async Task<IEnumerable<Cluster>> GetClustersByDatacenterAndEnvironmentAsync(string datacenter, string environment)
        {
            // Return some dummy clusters.
            return await Task.FromResult(new List<Cluster>
            {
                new Cluster
                {
                    ClusterId = "cluster-001",
                    ClusterName = $"{datacenter}-ClusterA",
                    Datacenter = datacenter,
                    Environment = environment,
                    AvailableCapacity = 100,
                    BaseUri = "https://api.clusterA.example.com"
                },
                new Cluster
                {
                    ClusterId = "cluster-002",
                    ClusterName = $"{datacenter}-ClusterB",
                    Datacenter = datacenter,
                    Environment = environment,
                    AvailableCapacity = 80,
                    BaseUri = "https://api.clusterB.example.com"
                }
            });
        }

        public async Task<IEnumerable<string>> GetDatabaseNamesByClusterAsync(string clusterId, string appId, string lob, string environment, string datacenter)
        {
            // Return a dummy list simulating that a database "myapp-fin-prod-dc1-1" already exists.
            // In a real implementation, call an API or query a database.
            if (clusterId == "cluster-001")
                return await Task.FromResult(new List<string> { $"{appId}-{lob}-{environment}-{datacenter}-1" });
            return await Task.FromResult(new List<string>());
        }
    }
}

namespace Infrastructure.Services
{
    using Domain.Interfaces;
    using Domain.Models;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;

    // A concrete implementation of IDatabaseCreator that uses HttpClient.
    public class RedisDatabaseCreator : IDatabaseCreator
    {
        private readonly HttpClient _httpClient;
        public RedisDatabaseCreator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<BdbResponse> CreateBdbAsync(string baseUri, object payload)
        {
            // Call the v1/bdbs endpoint.
            var response = await _httpClient.PostAsJsonAsync($"{baseUri}/v1/bdbs", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BdbResponse>();
        }

        public async Task<CrdbResponse> CreateCrdbAsync(string baseUri, object payload)
        {
            // Call the v1/crdbs endpoint.
            var response = await _httpClient.PostAsJsonAsync($"{baseUri}/v1/crdbs", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CrdbResponse>();
        }
    }

    // Response models that match the external Redis API.
    public class BdbResponse
    {
        public string uid { get; set; }
        public string name { get; set; }
        public int port { get; set; }
        public int memory_size { get; set; }
        public string status { get; set; }
        public string transaction_id { get; set; }
        // ... other fields as needed.
    }

    public class CrdbResponse
    {
        public string crdb_guid { get; set; }
        public string name { get; set; }
        public string uid { get; set; }
        public string transaction_id { get; set; }
        // ... other fields as needed.
    }
}

// =====================
// Presentation Layer (API Controller)
// =====================

namespace Presentation.Controllers
{
    using Application.Commands;
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseProvisioningController : ControllerBase
    {
        private readonly IMediator _mediator;
        public DatabaseProvisioningController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> ProvisionDatabase([FromBody] ProvisionDatabaseCommand command)
        {
            var result = await _mediator.Send(command);
            if (result.IsSuccess)
                return Ok(result.Success);
            return BadRequest(result.Error);
        }
    }
}
