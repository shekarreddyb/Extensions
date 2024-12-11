using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OfficeOpenXml;

class AzureDevOpsCommitFetcher
{
    private static readonly HttpClient HttpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter your Azure DevOps Personal Access Token:");
        string azurePat = Console.ReadLine();

        Console.WriteLine("Enter your Azure DevOps base URL (e.g., https://{your-server}/tfs/{collection}/_apis/):");
        string baseUrl = Console.ReadLine();

        Console.WriteLine("Enter your Azure DevOps project name:");
        string project = Console.ReadLine();

        Console.WriteLine("Enter the start date (YYYY-MM-DD):");
        string startDate = Console.ReadLine();

        Console.WriteLine("Enter the end date (YYYY-MM-DD):");
        string endDate = Console.ReadLine();

        Console.WriteLine("Enter output Excel file path:");
        string outputFilePath = Console.ReadLine();

        var allCommits = new ConcurrentBag<CommitRecord>();

        Console.WriteLine($"Fetching repositories for project: {project}");
        var repositories = await GetRepositories(baseUrl, project, azurePat);

        await Task.WhenAll(repositories.Select(repo => ProcessRepository(baseUrl, project, repo, azurePat, startDate, endDate, allCommits)));

        Console.WriteLine("Writing data to Excel...");
        WriteToExcel(allCommits, outputFilePath);
        Console.WriteLine($"Data written to {outputFilePath}");
    }

    private static async Task ProcessRepository(string baseUrl, string project, Repository repo, string pat, string startDate, string endDate, ConcurrentBag<CommitRecord> allCommits)
    {
        Console.WriteLine($"Fetching branches for repository: {repo.Name}");
        var branches = await GetBranches(baseUrl, project, repo.Id, pat);

        await Task.WhenAll(branches.Select(branch => ProcessBranch(baseUrl, project, repo, branch, pat, startDate, endDate, allCommits)));
    }

    private static async Task ProcessBranch(string baseUrl, string project, Repository repo, string branch, string pat, string startDate, string endDate, ConcurrentBag<CommitRecord> allCommits)
    {
        Console.WriteLine($"Fetching commits for branch: {branch} in repository: {repo.Name}");
        var commits = await GetCommits(baseUrl, project, repo.Id, branch, pat, startDate, endDate);
        foreach (var commit in commits)
        {
            allCommits.Add(commit);
        }
    }

    private static async Task<List<Repository>> GetRepositories(string baseUrl, string project, string pat)
    {
        string url = $"{baseUrl}/git/repositories?api-version=6.0";
        return await FetchAllPages<Repository>(url, pat);
    }

    private static async Task<List<string>> GetBranches(string baseUrl, string project, string repositoryId, string pat)
    {
        string url = $"{baseUrl}/git/repositories/{repositoryId}/refs?filter=heads/&api-version=6.0";
        var refs = await FetchAllPages<BranchRef>(url, pat);
        return refs.Select(r => r.Name.Replace("refs/heads/", "")).ToList();
    }

    private static async Task<List<CommitRecord>> GetCommits(string baseUrl, string project, string repositoryId, string branch, string pat, string startDate, string endDate)
    {
        string url = $"{baseUrl}/git/repositories/{repositoryId}/commits?searchCriteria.itemVersion.version={branch}&searchCriteria.fromDate={startDate}&searchCriteria.toDate={endDate}&api-version=6.0";

        var commitData = await FetchAllPages<Commit>(url, pat);
        return commitData.Select(commit => new CommitRecord
        {
            Repository = repositoryId,
            Author = commit.Author.Name,
            Branch = branch,
            Date = commit.Author.Date
        }).ToList();
    }

    private static async Task<List<T>> FetchAllPages<T>(string url, string pat)
    {
        var results = new List<T>();

        while (!string.IsNullOrEmpty(url))
        {
            HttpClient.DefaultRequestHeaders.Clear();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(":" + pat))}");
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "CSharp-AzureDevOps-API");

            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var pageData = JsonSerializer.Deserialize<ApiResponse<T>>(jsonResponse);
            if (pageData?.Value != null)
                results.AddRange(pageData.Value);

            url = pageData?.ContinuationToken != null ? $"{url}&continuationToken={pageData.ContinuationToken}" : null;
        }

        return results;
    }

    private static void WriteToExcel(ConcurrentBag<CommitRecord> commits, string filePath)
    {
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Azure DevOps Commits");

            worksheet.Cells[1, 1].Value = "Repository";
            worksheet.Cells[1, 2].Value = "User";
            worksheet.Cells[1, 3].Value = "Branch";
            worksheet.Cells[1, 4].Value = "Date";

            int row = 2;
            foreach (var commit in commits)
            {
                worksheet.Cells[row, 1].Value = commit.Repository;
                worksheet.Cells[row, 2].Value = commit.Author;
                worksheet.Cells[row, 3].Value = commit.Branch;
                worksheet.Cells[row, 4].Value = commit.Date.ToString("yyyy-MM-dd");
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }
    }
}

class Repository
{
    public string Id { get; set; }
    public string Name { get; set; }
}

class BranchRef
{
    public string Name { get; set; }
}

class Commit
{
    public CommitAuthor Author { get; set; }
}

class CommitAuthor
{
    public string Name { get; set; }
    public DateTime Date { get; set; }
}

class CommitRecord
{
    public string Repository { get; set; }
    public string Author { get; set; }
    public string Branch { get; set; }
    public DateTime Date { get; set; }
}

class ApiResponse<T>
{
    public List<T> Value { get; set; }
    public string ContinuationToken { get; set; }
}