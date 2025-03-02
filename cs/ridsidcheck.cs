using System;
using System.DirectoryServices;
using System.Security.Principal;

namespace PrimaryGroupArtifactCheck
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter the LDAP root (e.g., LDAP://DC=YourDomain,DC=com): ");
            string ldapRoot = Console.ReadLine();

            Console.Write("Enter the distinguished name (DN) of the group: ");
            string groupDn = Console.ReadLine();

            CheckPrimaryGroupArtifact(ldapRoot, groupDn);

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Checks if the group's primaryGroupToken matches its RID (the last sub-authority of its objectSid),
        /// which is the expected behavior for primary groups.
        /// </summary>
        /// <param name="ldapRoot">The base LDAP path (e.g., LDAP://DC=YourDomain,DC=com)</param>
        /// <param name="groupDn">The full distinguished name of the group to check.</param>
        static void CheckPrimaryGroupArtifact(string ldapRoot, string groupDn)
        {
            try
            {
                using (DirectoryEntry root = new DirectoryEntry(ldapRoot))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(root))
                    {
                        // Use a filter to match the group by its distinguishedName.
                        searcher.Filter = $"(&(objectClass=group)(distinguishedName={EscapeLdapSearchFilter(groupDn)}))";
                        searcher.SearchScope = SearchScope.Subtree;
                        // Request the attributes we need.
                        searcher.PropertiesToLoad.Add("primaryGroupToken");
                        searcher.PropertiesToLoad.Add("objectSid");

                        SearchResult result = searcher.FindOne();
                        if (result == null)
                        {
                            Console.WriteLine("Group not found.");
                            return;
                        }

                        if (result.Properties["primaryGroupToken"].Count == 0)
                        {
                            Console.WriteLine("primaryGroupToken not found on this group.");
                            return;
                        }
                        int primaryGroupToken = (int)result.Properties["primaryGroupToken"][0];

                        if (result.Properties["objectSid"].Count == 0)
                        {
                            Console.WriteLine("objectSid not found on this group.");
                            return;
                        }
                        byte[] sidBytes = (byte[])result.Properties["objectSid"][0];
                        SecurityIdentifier sid = new SecurityIdentifier(sidBytes, 0);
                        string sidString = sid.Value; // e.g., "S-1-5-21-1234567890-1234567890-1234567890-1234"
                        string[] parts = sidString.Split('-');
                        int rid = int.Parse(parts[parts.Length - 1]);

                        Console.WriteLine($"primaryGroupToken: {primaryGroupToken}");
                        Console.WriteLine($"Extracted RID from objectSid: {rid}");

                        if (primaryGroupToken == rid)
                        {
                            Console.WriteLine("The group's primaryGroupToken matches its RID. This is the primary group artifact.");
                        }
                        else
                        {
                            Console.WriteLine("The group's primaryGroupToken does NOT match its RID.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Escapes special characters for LDAP search filters per RFC 2254.
        /// </summary>
        static string EscapeLdapSearchFilter(string input)
        {
            if (input == null)
                return null;
            return input.Replace("\\", "\\5c")
                        .Replace("*", "\\2a")
                        .Replace("(", "\\28")
                        .Replace(")", "\\29")
                        .Replace("\0", "\\00");
        }
    }
}