using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OfficeOpenXml;

class GitHubCommitFetcher
{
    private static readonly HttpClient HttpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.WriteLine("Enter your GitHub Personal Access Token:");
        string gitHubToken = Console.ReadLine();

        Console.WriteLine("Enter GitHub organizations (comma-separated):");
        string[] organizations = Console.ReadLine()?.Split(',');

        Console.WriteLine("Enter GitHub Enterprise API base URL (e.g., https://github.mycompany.com/api/v3):");
        string baseUrl = Console.ReadLine();

        Console.WriteLine("Enter the start date (YYYY-MM-DD):");
        string startDate = Console.ReadLine();

        Console.WriteLine("Enter the end date (YYYY-MM-DD):");
        string endDate = Console.ReadLine();

        Console.WriteLine("Enter output Excel file path:");
        string outputFilePath = Console.ReadLine();

        var allCommits = new ConcurrentBag<CommitRecord>();

        // Process organizations in parallel
        await Task.WhenAll(organizations.Select(org => ProcessOrganization(org.Trim(), gitHubToken, baseUrl, startDate, endDate, allCommits)));

        Console.WriteLine("Writing data to Excel...");
        WriteToExcel(allCommits, outputFilePath);
        Console.WriteLine($"Data written to {outputFilePath}");
    }

    private static async Task ProcessOrganization(string organization, string token, string baseUrl, string startDate, string endDate, ConcurrentBag<CommitRecord> allCommits)
    {
        Console.WriteLine($"Fetching repositories for organization: {organization}");
        var repositories = await GetRepositories(organization, token, baseUrl);

        // Process repositories in parallel
        await Task.WhenAll(repositories.Select(repo => ProcessRepository(organization, repo, token, baseUrl, startDate, endDate, allCommits)));
    }

    private static async Task ProcessRepository(string organization, string repository, string token, string baseUrl, string startDate, string endDate, ConcurrentBag<CommitRecord> allCommits)
    {
        Console.WriteLine($"Fetching branches for repository: {repository}");
        var branches = await GetBranches(organization, repository, token, baseUrl);

        // Process branches in parallel
        await Task.WhenAll(branches.Select(branch => ProcessBranch(organization, repository, branch, token, baseUrl, startDate, endDate, allCommits)));
    }

    private static async Task ProcessBranch(string organization, string repository, string branch, string token, string baseUrl, string startDate, string endDate, ConcurrentBag<CommitRecord> allCommits)
    {
        Console.WriteLine($"Fetching commits for branch: {branch} in repository: {repository}");
        var commits = await GetCommits(organization, repository, branch, token, baseUrl, startDate, endDate);
        foreach (var commit in commits)
        {
            allCommits.Add(commit);
        }
    }

    private static async Task<List<string>> GetRepositories(string organization, string token, string baseUrl)
    {
        string url = $"{baseUrl}/orgs/{organization}/repos";
        return await FetchAllPages<Repository>(url, token)
            .ContinueWith(task => task.Result.Select(repo => repo.Name).ToList());
    }

    private static async Task<List<string>> GetBranches(string organization, string repository, string token, string baseUrl)
    {
        string url = $"{baseUrl}/repos/{organization}/{repository}/branches";
        return await FetchAllPages<Branch>(url, token)
            .ContinueWith(task => task.Result.Select(branch => branch.Name).ToList());
    }

    private static async Task<List<CommitRecord>> GetCommits(string organization, string repository, string branch, string token, string baseUrl, string startDate, string endDate)
    {
        string url = $"{baseUrl}/repos/{organization}/{repository}/commits?sha={branch}&since={startDate}T00:00:00Z&until={endDate}T23:59:59Z";
        var commitData = await FetchAllPages<Commit>(url, token);

        return commitData.Select(commit => new CommitRecord
        {
            Organization = organization,
            Repository = repository,
            Author = commit.Author?.Login,
            Branch = branch,
            Date = commit.CommitDetails.Author.Date
        }).ToList();
    }

    private static async Task<List<T>> FetchAllPages<T>(string baseUrl, string token)
    {
        var results = new List<T>();
        var url = baseUrl;

        while (!string.IsNullOrEmpty(url))
        {
            HttpClient.DefaultRequestHeaders.Clear();
            HttpClient.DefaultRequestHeaders.Add("Authorization", $"token {token}");
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "CSharp-GitHub-API");

            var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var pageData = JsonSerializer.Deserialize<List<T>>(jsonResponse);
            if (pageData != null) results.AddRange(pageData);

            // Check for the `Link` header to find the next page
            if (response.Headers.TryGetValues("Link", out var linkHeaders))
            {
                var linkHeader = linkHeaders.FirstOrDefault();
                url = ParseNextPageUrl(linkHeader);
            }
            else
            {
                url = null; // No more pages
            }
        }

        return results;
    }

    private static string ParseNextPageUrl(string linkHeader)
    {
        if (string.IsNullOrEmpty(linkHeader)) return null;

        var links = linkHeader.Split(',');
        foreach (var link in links)
        {
            var parts = link.Split(';');
            if (parts.Length == 2 && parts[1].Contains("rel=\"next\""))
            {
                return parts[0].Trim().Trim('<', '>');
            }
        }

        return null;
    }

    private static void WriteToExcel(ConcurrentBag<CommitRecord> commits, string filePath)
    {
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("GitHub Commits");

            worksheet.Cells[1, 1].Value = "Organization";
            worksheet.Cells[1, 2].Value = "Repository";
            worksheet.Cells[1, 3].Value = "User";
            worksheet.Cells[1, 4].Value = "Branch";
            worksheet.Cells[1, 5].Value = "Date";

            int row = 2;
            foreach (var commit in commits)
            {
                worksheet.Cells[row, 1].Value = commit.Organization;
                worksheet.Cells[row, 2].Value = commit.Repository;
                worksheet.Cells[row, 3].Value = commit.Author;
                worksheet.Cells[row, 4].Value = commit.Branch;
                worksheet.Cells[row, 5].Value = commit.Date.ToString("yyyy-MM-dd");
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }
    }
}

class Repository
{
    public string Name { get; set; }
}

class Branch
{
    public string Name { get; set; }
}

class Commit
{
    public CommitAuthor Author { get; set; }
    public CommitDetails CommitDetails { get; set; }
}

class CommitAuthor
{
    public string Login { get; set; }
}

class CommitDetails
{
    public CommitAuthorDetails Author { get; set; }
}

class CommitAuthorDetails
{
    public DateTime Date { get; set; }
}

class CommitRecord
{
    public string Organization { get; set; }
    public string Repository { get; set; }
    public string Author { get; set; }
    public string Branch { get; set; }
    public DateTime Date { get; set; }
}