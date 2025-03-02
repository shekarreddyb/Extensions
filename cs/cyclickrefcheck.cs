using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace CircularNestedGroupsChecker
{
    class Program
    {
        // Global variable to hold the LDAP root provided by the user.
        static string ldapRoot;

        static void Main(string[] args)
        {
            Console.Write("Enter the LDAP path of your domain (e.g., LDAP://DC=YourDomain,DC=com): ");
            ldapRoot = Console.ReadLine();

            Console.Write("Enter the distinguished name (DN) of the group to check: ");
            string groupDn = Console.ReadLine();

            Console.WriteLine("\nChecking for circular nested group references...\n");

            // Start the DFS from the target group; if a cycle is detected, it'll be reported.
            bool cycleFound = DetectCycle(groupDn, groupDn, new List<string>());

            if (cycleFound)
            {
                Console.WriteLine("\nCircular nested group reference detected.");
            }
            else
            {
                Console.WriteLine("\nNo circular nested group reference found.");
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Recursively checks for a cycle starting from 'startGroupDn' in the membership chain.
        /// </summary>
        static bool DetectCycle(string startGroupDn, string currentGroupDn, List<string> path)
        {
            if (path.Contains(currentGroupDn, StringComparer.OrdinalIgnoreCase))
            {
                // Only flag as a cycle if it loops back to the starting group.
                if (string.Equals(currentGroupDn, startGroupDn, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cycle detected: " + string.Join(" -> ", path) + " -> " + currentGroupDn);
                    return true;
                }
                return false;
            }

            // Add the current group to the current DFS path.
            path.Add(currentGroupDn);

            // Get direct nested groups using DirectorySearcher with a filter.
            List<string> nestedGroups = GetDirectGroupMembers(currentGroupDn);

            foreach (string nestedDn in nestedGroups)
            {
                if (DetectCycle(startGroupDn, nestedDn, new List<string>(path)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves the direct group members (nested groups) for a given group DN.
        /// </summary>
        static List<string> GetDirectGroupMembers(string groupDn)
        {
            List<string> result = new List<string>();

            try
            {
                DirectoryEntry groupEntry = GetGroupEntry(groupDn);
                if (groupEntry != null && groupEntry.Properties["member"] != null)
                {
                    foreach (object member in groupEntry.Properties["member"])
                    {
                        string memberDn = member.ToString();
                        if (IsGroup(memberDn))
                        {
                            result.Add(memberDn);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving members for group {0}: {1}", groupDn, ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Retrieves a DirectoryEntry for a group using DirectorySearcher with a filter on distinguishedName.
        /// </summary>
        static DirectoryEntry GetGroupEntry(string groupDn)
        {
            try
            {
                // Bind to the base LDAP path.
                using (DirectoryEntry rootEntry = new DirectoryEntry(ldapRoot))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(rootEntry))
                    {
                        // Use a filter to match the exact distinguishedName.
                        searcher.Filter = $"(&(objectClass=group)(distinguishedName={EscapeLdapSearchFilter(groupDn)}))";
                        searcher.SearchScope = SearchScope.Subtree;
                        // Load properties we need.
                        searcher.PropertiesToLoad.Add("member");
                        searcher.PropertiesToLoad.Add("objectClass");

                        SearchResult result = searcher.FindOne();
                        if (result != null)
                        {
                            return result.GetDirectoryEntry();
                        }
                        else
                        {
                            Console.WriteLine("Group not found: " + groupDn);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving group entry for {0}: {1}", groupDn, ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Checks whether the specified DN represents a group by retrieving its objectClass.
        /// </summary>
        static bool IsGroup(string dn)
        {
            try
            {
                using (DirectoryEntry entry = GetGroupEntry(dn))
                {
                    if (entry != null && entry.Properties["objectClass"] != null)
                    {
                        foreach (object o in entry.Properties["objectClass"])
                        {
                            if (o.ToString().Equals("group", StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking if DN is a group ({0}): {1}", dn, ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Escapes special characters in the LDAP search filter per RFC2254.
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