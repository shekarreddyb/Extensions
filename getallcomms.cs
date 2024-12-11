using System;
using System.Collections.Generic;
using System.IO;
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

        var commitsData = new List<CommitRecord>();

        foreach (var org in organizations)
        {
            string trimmedOrg = org.Trim();
            Console.WriteLine($"Fetching repositories for organization: {trimmedOrg}");
            var repositories = await GetRepositories(trimmedOrg, gitHubToken, baseUrl);

            foreach (var repo in repositories)
            {
                Console.WriteLine($"Fetching branches for repository: {repo}");
                var branches = await GetBranches(trimmedOrg, repo, gitHubToken, baseUrl);

                foreach (var branch in branches)
                {
                    Console.WriteLine($"Fetching commits for branch: {branch} in repository: {repo}");
                    var branchCommits = await GetCommits(trimmedOrg, repo, branch, gitHubToken, baseUrl, startDate, endDate);
                    commitsData.AddRange(branchCommits);
                }
            }
        }

        Console.WriteLine("Writing data to Excel...");
        WriteToExcel(commitsData, outputFilePath);
        Console.WriteLine($"Data written to {outputFilePath}");
    }

    private static async Task<List<string>> GetRepositories(string organization, string token, string baseUrl)
    {
        string url = $"{baseUrl}/orgs/{organization}/repos";
        HttpClient.DefaultRequestHeaders.Add("Authorization", $"token {token}");
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "CSharp-GitHub-API");

        HttpResponseMessage response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var repos = JsonSerializer.Deserialize<List<Repository>>(await response.Content.ReadAsStringAsync());
        return repos?.ConvertAll(repo => repo.Name);
    }

    private static async Task<List<string>> GetBranches(string organization, string repository, string token, string baseUrl)
    {
        string url = $"{baseUrl}/repos/{organization}/{repository}/branches";
        HttpClient.DefaultRequestHeaders.Clear();
        HttpClient.DefaultRequestHeaders.Add("Authorization", $"token {token}");
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "CSharp-GitHub-API");

        HttpResponseMessage response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var branches = JsonSerializer.Deserialize<List<Branch>>(await response.Content.ReadAsStringAsync());
        return branches?.ConvertAll(branch => branch.Name);
    }

    private static async Task<List<CommitRecord>> GetCommits(
        string organization,
        string repository,
        string branch,
        string token,
        string baseUrl,
        string startDate,
        string endDate)
    {
        var commits = new List<CommitRecord>();
        string url = $"{baseUrl}/repos/{organization}/{repository}/commits?sha={branch}&since={startDate}T00:00:00Z&until={endDate}T23:59:59Z";

        HttpClient.DefaultRequestHeaders.Clear();
        HttpClient.DefaultRequestHeaders.Add("Authorization", $"token {token}");
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "CSharp-GitHub-API");

        HttpResponseMessage response = await HttpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error fetching commits for {repository}/{branch}: {response.StatusCode}");
            return commits;
        }

        var commitsData = JsonSerializer.Deserialize<List<Commit>>(await response.Content.ReadAsStringAsync());
        if (commitsData != null)
        {
            foreach (var commit in commitsData)
            {
                if (commit.Author?.Login != null)
                {
                    commits.Add(new CommitRecord
                    {
                        Organization = organization,
                        Repository = repository,
                        Author = commit.Author.Login,
                        Branch = branch,
                        Date = commit.CommitDetails.Author.Date
                    });
                }
            }
        }

        return commits;
    }

    private static void WriteToExcel(List<CommitRecord> commits, string filePath)
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