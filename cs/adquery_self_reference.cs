using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Globalization;

class Program
{
    static void Main()
    {
        string domainPath = "LDAP://OU=YourOU,DC=targetdomain,DC=com"; // Change to target domain OU
        string username = "yourusername@targetdomain.com"; // Use a valid account in the target domain
        string password = "yourpassword"; // Ensure this is secure (consider using secure storage)
        string outputFilePath = "GroupsList.txt"; // Output file

        List<string> groups = GetGroupsInOU(domainPath, username, password);

        // Write groups to a text file
        File.WriteAllLines(outputFilePath, groups);

        Console.WriteLine($"Groups have been written to {outputFilePath}");
    }

    static List<string> GetGroupsInOU(string ouPath, string username, string password)
    {
        List<string> groups = new();
        DirectoryEntry entry = new(ouPath, username, password); // Use explicit credentials
        DirectorySearcher searcher = new(entry)
        {
            Filter = "(objectClass=group)",
            SearchScope = SearchScope.Subtree // Search all nested OUs
        };

        searcher.PropertiesToLoad.Add("cn"); // Group name
        searcher.PropertiesToLoad.Add("whenCreated"); // Creation date

        foreach (SearchResult result in searcher.FindAll())
        {
            string groupName = result.Properties.Contains("cn") ? result.Properties["cn"][0].ToString() : "Unknown Group";
            string whenCreated = result.Properties.Contains("whenCreated") 
                ? DateTime.ParseExact(result.Properties["whenCreated"][0].ToString(), "yyyyMMddHHmmss.0Z", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss")
                : "Unknown Date";

            groups.Add($"{groupName}, {whenCreated}");
        }

        return groups;
    }
}