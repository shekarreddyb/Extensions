using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Globalization;

class Program
{
    static void Main()
    {
        string domain = "LDAP://OU=YourOU,DC=yourdomain,DC=com"; // Change this to your OU
        string outputFilePath = "GroupsList.txt"; // File to save the group names

        List<string> groups = GetGroupsInOU(domain);

        // Write groups to a text file
        File.WriteAllLines(outputFilePath, groups);

        Console.WriteLine($"Groups have been written to {outputFilePath}");
    }

    static List<string> GetGroupsInOU(string ouPath)
    {
        List<string> groups = new();
        DirectoryEntry entry = new(ouPath);
        DirectorySearcher searcher = new(entry)
        {
            Filter = "(objectClass=group)",
            SearchScope = SearchScope.Subtree // Ensures it searches all nested OUs
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