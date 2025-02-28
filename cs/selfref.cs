using System;
using System.DirectoryServices;

class Program
{
    static void Main()
    {
        string domainPath = "LDAP://targetdomain.com"; // Change to your domain
        string groupName = "YourGroupName"; // Change to the group you want to check
        string username = "yourusername@targetdomain.com"; // AD username
        string password = "yourpassword"; // AD password

        bool isPartOfItself = IsGroupMemberOfItself(domainPath, groupName, username, password);

        Console.WriteLine($"Is '{groupName}' a member of itself? {isPartOfItself}");
    }

    static bool IsGroupMemberOfItself(string domainPath, string groupName, string username, string password)
    {
        try
        {
            using DirectoryEntry entry = new(domainPath, username, password);
            using DirectorySearcher searcher = new(entry)
            {
                Filter = $"(&(objectClass=group)(cn={groupName}))",
                SearchScope = SearchScope.Subtree
            };

            searcher.PropertiesToLoad.Add("distinguishedName");
            searcher.PropertiesToLoad.Add("member");

            SearchResult result = searcher.FindOne();
            if (result == null)
            {
                Console.WriteLine("Group not found.");
                return false;
            }

            string groupDN = result.Properties["distinguishedName"][0].ToString(); // Get DN of group

            if (result.Properties.Contains("member"))
            {
                foreach (var member in result.Properties["member"])
                {
                    if (member.ToString().Equals(groupDN, StringComparison.OrdinalIgnoreCase))
                    {
                        return true; // Group is a member of itself
                    }
                }
            }

            return false; // Group is not a member of itself
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }
}