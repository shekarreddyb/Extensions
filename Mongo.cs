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






using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

public SortDefinition<BsonDocument> CreateSortDefinition(string jsonSort)
{
    var sortDefinitionBuilder = Builders<BsonDocument>.Sort;
    SortDefinition<BsonDocument> sortDefinition = null;

    var sortParams = BsonDocument.Parse(jsonSort);
    foreach (var sortParam in sortParams)
    {
        var direction = sortParam.Value.AsInt32 == 1 ?
                        sortDefinitionBuilder.Ascending(sortParam.Name) :
                        sortDefinitionBuilder.Descending(sortParam.Name);

        sortDefinition = sortDefinition == null ? direction : sortDefinition.Combine(direction);
    }

    return sortDefinition;
}


using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

public SortDefinition<BsonDocument> CreateSortDefinition(string jsonSort)
{
    var sortDefinitionBuilder = Builders<BsonDocument>.Sort;
    var sortDefinitions = new List<SortDefinition<BsonDocument>>();
    
    // Parse the JSON to get sort fields and their order
    var sortFields = JObject.Parse(jsonSort);

    foreach (var field in sortFields)
    {
        // Determine the sort order for each field
        var fieldName = field.Key;
        var sortOrder = field.Value.ToObject<int>();
        
        var sortDefinition = sortOrder == 1 
            ? sortDefinitionBuilder.Ascending(fieldName) 
            : sortDefinitionBuilder.Descending(fieldName);
        
        sortDefinitions.Add(sortDefinition);
    }

    // Combine all sort definitions
    var combinedSortDefinition = sortDefinitionBuilder.Combine(sortDefinitions);
    
    return combinedSortDefinition;
}
