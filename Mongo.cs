using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using RiskFirst.RestClient.Pagination;
using System;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
var connectionString = "your_mongodb_connection_string";
var mongoClient = new MongoClient(connectionString);

var app = builder.Build();

app.MapGet("/data", async (string collectionName, string sortBy, int page, int pageSize) =>
{
    // Validate inputs
    if (string.IsNullOrWhiteSpace(collectionName) || string.IsNullOrWhiteSpace(sortBy) || page <= 0 || pageSize <= 0)
        return Results.BadRequest("Invalid parameters");

    // Split sortBy to get field and sort order
    var sortByParams = sortBy.Split(',');
    if (sortByParams.Length != 2 || (sortByParams[1].ToLower() != "asc" && sortByParams[1].ToLower() != "desc"))
        return Results.BadRequest("Invalid sortBy parameter");

    var sortField = sortByParams[0];
    var isAscending = sortByParams[1].ToLower() == "asc";

    var database = mongoClient.GetDatabase("YourDatabaseName");
    var collection = database.GetCollection<BsonDocument>(collectionName);

    var sortDefinition = isAscending
        ? Builders<BsonDocument>.Sort.Ascending(sortField)
        : Builders<BsonDocument>.Sort.Descending(sortField);

    var totalCount = await collection.CountDocumentsAsync(new BsonDocument());
    var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

    var documents = await collection.Find(new BsonDocument())
        .Sort(sortDefinition)
        .Skip((page - 1) * pageSize)
        .Limit(pageSize)
        .ToListAsync();

    var pageInfo = new PageInfo
    {
        CurrentPage = page,
        ItemsPerPage = pageSize,
        TotalItems = totalCount,
        TotalPages = totalPages
    };

    return Results.Ok(new PaginatedResponse<BsonDocument>
    {
        Data = documents,
        PageInfo = pageInfo
    });
});

app.Run();
