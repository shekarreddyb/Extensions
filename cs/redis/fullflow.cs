using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

#region Domain Layer

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
        // For simplicity, each cluster includes its base URI.
        public string BaseUri { get; set; }
    }
}

namespace Domain.Interfaces
{
    using Domain.Entities;
    using Domain.Models;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    // Abstraction to fetch clusters and query database names.
    public interface IClusterRepository
    {
        Task<IEnumerable<Cluster>> GetClustersByDatacenterAndEnvironmentAsync(string datacenter, string environment);
        Task<IEnumerable<string>> GetDatabaseNamesByClusterAsync(string clusterId, string appId, string lob, string environment, string datacenter);
    }

    // Abstraction to provision a database (BDB or CRDB).
    public interface IDatabaseCreator
    {
        Task<Infrastructure.Services.BdbResponse> CreateBdbAsync(string baseUri, object payload);
        Task<Infrastructure.Services.CrdbResponse> CreateCrdbAsync(string baseUri, object payload);
    }

    // Abstraction for saving and retrieving the persisted database record.
    public interface IRedisDatabaseRecordRepository
    {
        Task<DatabaseProvisioningResult> SaveDatabaseRecordAsync(DatabaseProvisioningResult record);
        Task<DatabaseProvisioningResult> GetDatabaseRecordAsync(string guid);
    }
}

namespace Domain.Models
{
    // Domain model for the provisioning result record.
    public class DatabaseProvisioningResult
    {
        public string Guid { get; set; }
        public string AppId { get; set; }
        public string Lob { get; set; }
        public DateTime CreatedOn { get; set; }
        public string TransactionId { get; set; }
        public string DbType { get; set; }  // "BDB" or "CRDB"
        public string ClusterName { get; set; }
        public string DbName { get; set; }
        public bool IsCrdb { get; set; }
        public string DatabaseUid { get; set; }
        public string CrdbGuid { get; set; }
        public TlsSettings TlsSettings { get; set; }
    }
}

namespace Domain
{
    // Value object for TLS settings.
    public class TlsSettings
    {
        public string OU { get; set; }
        public string CN { get; set; }
    }
}

namespace Domain
{
    // Generic Result wrapper (monad) for success/error handling.
    public class Result<TSuccess, TError>
    {
        public TSuccess Success { get; }
        public TError Error { get; }
        public bool IsSuccess { get; }
        private Result(TSuccess success) { Success = success; IsSuccess = true; }
        private Result(TError error) { Error = error; IsSuccess = false; }
        public static Result<TSuccess, TError> Ok(TSuccess success) => new Result<TSuccess, TError>(success);
        public static Result<TSuccess, TError> Fail(TError error) => new Result<TSuccess, TError>(error);
    }
}

namespace Domain.Services
{
    using Domain.Interfaces;
    using Domain.Models;
    using Application.Commands;
    using System.Threading.Tasks;

    // Domain service that encapsulates business logic for provisioning.
    public class DatabaseProvisioningDomainService
    {
        private readonly IClusterRepository _clusterRepository;
        private readonly IDatabaseCreator _databaseCreator;
        private readonly IRedisDatabaseRecordRepository _recordRepository;

        public DatabaseProvisioningDomainService(
            IClusterRepository clusterRepository,
            IDatabaseCreator databaseCreator,
            IRedisDatabaseRecordRepository recordRepository)
        {
            _clusterRepository = clusterRepository;
            _databaseCreator = databaseCreator;
            _recordRepository = recordRepository;
        }

        public async Task<Result<List<DatabaseProvisioningResult>, List<string>>> ProvisionDatabasesAsync(ProvisionDatabaseCommand command)
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

                // 2. Choose the best cluster based on available capacity.
                var bestCluster = clusters.OrderByDescending(c => c.AvailableCapacity).First();

                // 3. Form the base database name.
                string baseDbName = $"{command.AppId}-{command.Lob}-{command.Environment}-{dc}";
                // 4. Query existing database names to determine sequence number.
                var existingNames = await _clusterRepository.GetDatabaseNamesByClusterAsync(bestCluster.ClusterId, command.AppId, command.Lob, command.Environment, dc);
                int seq = existingNames.Any() ? existingNames.Select(n =>
                {
                    var parts = n.Split('-');
                    return int.TryParse(parts.Last(), out int number) ? number : 0;
                }).Max() + 1 : 1;
                string dbName = $"{baseDbName}-{seq}";

                // 5. Depending on IsCrdb flag, call the appropriate Redis API.
                if (!command.IsCrdb)
                {
                    var bdbPayload = new
                    {
                        name = dbName,
                        memory_size = command.MemorySize,
                        port = 6379,
                        replication = true,
                        aof_persistence = false,
                        tls_enabled = true,
                        module = command.Module
                    };

                    try
                    {
                        var bdbResponse = await _databaseCreator.CreateBdbAsync(bestCluster.BaseUri, bdbPayload);
                        var record = new DatabaseProvisioningResult
                        {
                            Guid = Guid.NewGuid().ToString(),
                            AppId = command.AppId,
                            Lob = command.Lob,
                            CreatedOn = DateTime.UtcNow,
                            TransactionId = bdbResponse.transaction_id,
                            DbType = "BDB",
                            ClusterName = bestCluster.ClusterName,
                            DbName = bdbResponse.name,
                            IsCrdb = false,
                            DatabaseUid = bdbResponse.uid,
                            CrdbGuid = null,
                            TlsSettings = new TlsSettings { OU = command.TlsSettings.OU, CN = command.TlsSettings.CN }
                        };
                        var savedRecord = await _recordRepository.SaveDatabaseRecordAsync(record);
                        results.Add(savedRecord);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error creating BDB in datacenter {dc}: {ex.Message}");
                    }
                }
                else
                {
                    // For CRDB, gather instance cluster IDs.
                    var clustersInDc = clusters.ToList();
                    var instanceClusterIds = clustersInDc.Select(c => c.ClusterId).ToList();
                    var crdbPayload = new
                    {
                        name = dbName,
                        regions = instanceClusterIds,
                        port = 6380,
                        replication = true
                    };

                    try
                    {
                        var crdbResponse = await _databaseCreator.CreateCrdbAsync(bestCluster.BaseUri, crdbPayload);
                        var record = new DatabaseProvisioningResult
                        {
                            Guid = Guid.NewGuid().ToString(),
                            AppId = command.AppId,
                            Lob = command.Lob,
                            CreatedOn = DateTime.UtcNow,
                            TransactionId = crdbResponse.transaction_id,
                            DbType = "CRDB",
                            ClusterName = bestCluster.ClusterName,
                            DbName = crdbResponse.name,
                            IsCrdb = true,
                            DatabaseUid = crdbResponse.uid,
                            CrdbGuid = crdbResponse.crdb_guid,
                            TlsSettings = new TlsSettings { OU = command.TlsSettings.OU, CN = command.TlsSettings.CN }
                        };
                        var savedRecord = await _recordRepository.SaveDatabaseRecordAsync(record);
                        results.Add(savedRecord);
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

#endregion

#region Application Layer

namespace Application.Commands
{
    using MediatR;
    using System.Collections.Generic;
    using Domain;

    // Combined DTO/command for provisioning a database.
    public class ProvisionDatabaseCommand : IRequest<Domain.Result<ProvisionDatabaseResponse, List<string>>>
    {
        public string AppId { get; set; }
        public string Lob { get; set; }
        public string Environment { get; set; }
        public List<string> DatacentersList { get; set; }
        public bool IsCrdb { get; set; }
        public string Module { get; set; }  // Only used if IsCrdb is false.
        public int MemorySize { get; set; }
        public TlsSettings TlsSettings { get; set; }
    }

    // API response that will be returned.
    public class ProvisionDatabaseResponse
    {
        public string AppId { get; set; }
        public string Lob { get; set; }
        public DateTime CreatedOn { get; set; }
        public string TransactionId { get; set; }
        public string DbType { get; set; }
        public string ClusterName { get; set; }
        public string DbName { get; set; }
        public bool IsCrdb { get; set; }
        public string DatabaseUid { get; set; }
        public string CrdbGuid { get; set; }
        public TlsSettings TlsSettings { get; set; }
    }
}

namespace Application.Validators
{
    using FluentValidation;
    using Application.Commands;

    // FluentValidation rules on the combined command.
    public class ProvisionDatabaseCommandValidator : AbstractValidator<ProvisionDatabaseCommand>
    {
        public ProvisionDatabaseCommandValidator()
        {
            RuleFor(x => x.AppId).NotEmpty().WithMessage("AppId is required.");
            RuleFor(x => x.Lob).NotEmpty().WithMessage("LOB is required.");
            RuleFor(x => x.DatacentersList).NotEmpty().WithMessage("At least one datacenter is required.");
            RuleFor(x => x.Environment).NotEmpty().WithMessage("Environment is required.");
            RuleFor(x => x.MemorySize).GreaterThan(0).WithMessage("MemorySize must be greater than zero.");
            RuleFor(x => x.Module).NotEmpty().When(x => !x.IsCrdb).WithMessage("Module is required when IsCrdb is false.");
            RuleFor(x => x.TlsSettings).NotNull().WithMessage("TLS settings are required.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.TlsSettings.OU).NotEmpty().WithMessage("OU is required.");
                    RuleFor(x => x.TlsSettings.CN).NotEmpty().WithMessage("CN is required.");
                });
        }
    }
}

namespace Application.Behaviors
{
    using FluentValidation;
    using MediatR;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Domain;

    // Pipeline behavior that validates the command before handling.
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                var failures = results.SelectMany(r => r.Errors).Where(f => f != null).ToList();
                if (failures.Any())
                {
                    object response = Domain.Result<ProvisionDatabaseResponse, List<string>>.Fail(
                        failures.Select(f => f.ErrorMessage).ToList());
                    return (TResponse)response;
                }
            }
            return await next();
        }
    }
}

namespace Application.Handlers
{
    using MediatR;
    using Application.Commands;
    using Domain.Services;
    using Domain;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Collections.Generic;

    // MediatR handler that calls the domain service and maps results to the API response.
    public class ProvisionDatabaseCommandHandler : IRequestHandler<ProvisionDatabaseCommand, Domain.Result<ProvisionDatabaseResponse, List<string>>>
    {
        private readonly DatabaseProvisioningDomainService _domainService;
        public ProvisionDatabaseCommandHandler(DatabaseProvisioningDomainService domainService)
        {
            _domainService = domainService;
        }
        public async Task<Domain.Result<ProvisionDatabaseResponse, List<string>>> Handle(ProvisionDatabaseCommand command, CancellationToken cancellationToken)
        {
            var domainResult = await _domainService.ProvisionDatabasesAsync(command);
            if (domainResult.IsSuccess)
            {
                var first = domainResult.Success.First();
                var response = new ProvisionDatabaseResponse
                {
                    AppId = command.AppId,
                    Lob = command.Lob,
                    CreatedOn = DateTime.UtcNow,
                    TransactionId = first.TransactionId,
                    DbType = command.IsCrdb ? "CRDB" : "BDB",
                    ClusterName = first.ClusterName,
                    DbName = first.DbName,
                    IsCrdb = command.IsCrdb,
                    DatabaseUid = first.DatabaseUid,
                    CrdbGuid = first.CrdbGuid,
                    TlsSettings = new TlsSettings { OU = command.TlsSettings.OU, CN = command.TlsSettings.CN }
                };
                return Domain.Result<ProvisionDatabaseResponse, List<string>>.Ok(response);
            }
            return Domain.Result<ProvisionDatabaseResponse, List<string>>.Fail(domainResult.Error);
        }
    }
}

#endregion

#region Infrastructure Layer

namespace Infrastructure.Repositories
{
    using Domain.Interfaces;
    using Domain.Entities;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    // A dummy implementation that returns sample clusters and database names.
    public class ClusterRepository : IClusterRepository
    {
        public async Task<IEnumerable<Cluster>> GetClustersByDatacenterAndEnvironmentAsync(string datacenter, string environment)
        {
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
            if (clusterId == "cluster-001")
                return await Task.FromResult(new List<string> { $"{appId}-{lob}-{environment}-{datacenter}-1" });
            return await Task.FromResult(new List<string>());
        }
    }
}

namespace Infrastructure.Services
{
    using Domain.Interfaces;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;

    // Concrete implementation for calling the external Redis API.
    public class RedisDatabaseCreator : IDatabaseCreator
    {
        private readonly HttpClient _httpClient;
        public RedisDatabaseCreator(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<Services.BdbResponse> CreateBdbAsync(string baseUri, object payload)
        {
            var response = await _httpClient.PostAsJsonAsync($"{baseUri}/v1/bdbs", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Services.BdbResponse>();
        }
        public async Task<Services.CrdbResponse> CreateCrdbAsync(string baseUri, object payload)
        {
            var response = await _httpClient.PostAsJsonAsync($"{baseUri}/v1/crdbs", payload);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Services.CrdbResponse>();
        }
    }

    // Models matching the external API responses.
    public class BdbResponse
    {
        public string uid { get; set; }
        public string name { get; set; }
        public int port { get; set; }
        public int memory_size { get; set; }
        public string status { get; set; }
        public string transaction_id { get; set; }
    }

    public class CrdbResponse
    {
        public string crdb_guid { get; set; }
        public string name { get; set; }
        public string uid { get; set; }
        public string transaction_id { get; set; }
    }
}

namespace Infrastructure.Repositories
{
    using Domain.Interfaces;
    using Domain.Models;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    // Simple in-memory repository for persisting database records.
    public class InMemoryRedisDatabaseRecordRepository : IRedisDatabaseRecordRepository
    {
        private readonly ConcurrentDictionary<string, DatabaseProvisioningResult> _store = new ConcurrentDictionary<string, DatabaseProvisioningResult>();

        public async Task<DatabaseProvisioningResult> SaveDatabaseRecordAsync(DatabaseProvisioningResult record)
        {
            _store[record.Guid] = record;
            return await Task.FromResult(record);
        }

        public async Task<DatabaseProvisioningResult> GetDatabaseRecordAsync(string guid)
        {
            _store.TryGetValue(guid, out var record);
            return await Task.FromResult(record);
        }
    }
}

#endregion

#region Presentation Layer

namespace Presentation.Controllers
{
    using Application.Commands;
    using MediatR;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;

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

        // GET endpoint for retrieving all records.
        [HttpGet]
        public async Task<IActionResult> GetAllRecords()
        {
            // For demonstration, return dummy data.
            return Ok(new[] { "Record1", "Record2" });
        }

        // DELETE endpoint to remove a record by its GUID.
        [HttpDelete("{guid}")]
        public async Task<IActionResult> DeleteRecord(string guid)
        {
            // In a real app, call an application service to delete.
            return Ok(new { message = $"Database record with guid '{guid}' deleted successfully." });
        }

        // PATCH endpoint to update a record.
        [HttpPatch("{guid}")]
        public async Task<IActionResult> UpdateRecord(string guid, [FromBody] ProvisionDatabaseCommand command)
        {
            // For simplicity, we return the command as the updated record.
            return Ok(new
            {
                message = "Database record updated successfully.",
                updatedRecord = command
            });
        }

        // Search endpoint.
        [HttpGet("search")]
        public async Task<IActionResult> SearchRecords([FromQuery] SearchRedisRecordsQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsSuccess)
                return Ok(result.Success);
            return BadRequest(result.Error);
        }
    }

    // MediatR query for searching records.
    public class SearchRedisRecordsQuery : IRequest<Domain.Result<System.Collections.Generic.List<ProvisionDatabaseResponse>, string>>
    {
        public string AppId { get; set; }
        public string Lob { get; set; }
        public string TransactionId { get; set; }
    }
}

namespace Application.Handlers
{
    using MediatR;
    using Application.Commands;
    using Domain.Interfaces;
    using Domain;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Linq;

    // Handler for the search query.
    public class SearchRedisRecordsQueryHandler : IRequestHandler<SearchRedisRecordsQuery, Domain.Result<List<ProvisionDatabaseResponse>, string>>
    {
        private readonly IRedisDatabaseRecordRepository _repository;
        public SearchRedisRecordsQueryHandler(IRedisDatabaseRecordRepository repository)
        {
            _repository = repository;
        }
        public async Task<Domain.Result<List<ProvisionDatabaseResponse>, string>> Handle(SearchRedisRecordsQuery query, CancellationToken cancellationToken)
        {
            // Dummy implementation; in a real app, search the persisted records.
            var records = new List<ProvisionDatabaseResponse>
            {
                new ProvisionDatabaseResponse
                {
                    AppId = query.AppId,
                    Lob = query.Lob,
                    CreatedOn = System.DateTime.UtcNow,
                    TransactionId = query.TransactionId,
                    DbType = "BDB",
                    ClusterName = "dc1-ClusterA",
                    DbName = $"{query.AppId}-{query.Lob}-prod-dc1-1",
                    IsCrdb = false,
                    DatabaseUid = "BDB-12345",
                    CrdbGuid = null,
                    TlsSettings = new Domain.TlsSettings { OU = "OrgUnitA", CN = "CommonNameA" }
                }
            };
            if (records.Any())
                return Domain.Result<List<ProvisionDatabaseResponse>, string>.Ok(records);
            return Domain.Result<List<ProvisionDatabaseResponse>, string>.Fail("No matching records found.");
        }
    }
}

#endregion
