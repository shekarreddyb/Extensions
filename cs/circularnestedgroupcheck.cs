using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace CircularNestedGroupsChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            // Prompt for the LDAP root (optional if using full DNs) and the group's distinguished name.
            Console.Write("Enter the LDAP path of your domain (e.g., LDAP://DC=YourDomain,DC=com): ");
            string ldapRoot = Console.ReadLine();

            Console.Write("Enter the distinguished name (DN) of the group to check: ");
            string groupDn = Console.ReadLine();

            Console.WriteLine("\nChecking for circular nested group references...\n");

            // Start the cycle detection with the target group as both the starting point and the current node.
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
        /// Recursively performs a DFS starting from the currentGroupDn.
        /// If the original group (startGroupDn) is encountered again in the DFS path, a cycle is present.
        /// </summary>
        /// <param name="startGroupDn">The DN of the group being checked for a cycle.</param>
        /// <param name="currentGroupDn">The current group's DN in the DFS recursion.</param>
        /// <param name="path">A list tracking the DNs visited in the current recursion chain.</param>
        /// <returns>True if a cycle is detected; otherwise, false.</returns>
        static bool DetectCycle(string startGroupDn, string currentGroupDn, List<string> path)
        {
            // If the current group already exists in the path, we may have a cycle.
            if (path.Contains(currentGroupDn, StringComparer.OrdinalIgnoreCase))
            {
                // A cycle is detected only if we cycle back to the starting group.
                if (string.Equals(currentGroupDn, startGroupDn, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cycle detected: " + string.Join(" -> ", path) + " -> " + currentGroupDn);
                    return true;
                }
                return false;
            }

            // Add the current group to the DFS path.
            path.Add(currentGroupDn);

            // Retrieve all direct nested groups for the current group.
            List<string> nestedGroups = GetDirectGroupMembers(currentGroupDn);

            foreach (string nestedDn in nestedGroups)
            {
                // Recursively search each nested group.
                if (DetectCycle(startGroupDn, nestedDn, new List<string>(path)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieves the direct nested group members for a given group DN.
        /// </summary>
        /// <param name="groupDn">The distinguished name of the group to query.</param>
        /// <returns>A list of DNs representing the nested groups.</returns>
        static List<string> GetDirectGroupMembers(string groupDn)
        {
            List<string> result = new List<string>();

            try
            {
                // Bind to the group. If the DN doesn't start with "LDAP://", prepend it.
                using (DirectoryEntry groupEntry = new DirectoryEntry(GetLdapPath(groupDn)))
                {
                    if (groupEntry.Properties["member"] != null)
                    {
                        foreach (object member in groupEntry.Properties["member"])
                        {
                            string memberDn = member.ToString();
                            // Check if the member object is a group.
                            if (IsGroup(memberDn))
                            {
                                result.Add(memberDn);
                            }
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
        /// Returns a proper LDAP path for the given distinguished name.
        /// </summary>
        /// <param name="dn">The distinguished name.</param>
        /// <returns>The LDAP path (e.g., LDAP://CN=Group,OU=Groups,DC=YourDomain,DC=com).</returns>
        static string GetLdapPath(string dn)
        {
            if (dn.StartsWith("LDAP://", StringComparison.OrdinalIgnoreCase))
            {
                return dn;
            }
            else
            {
                return "LDAP://" + dn;
            }
        }

        /// <summary>
        /// Checks whether the object identified by the distinguished name is a group.
        /// </summary>
        /// <param name="dn">The distinguished name of the object.</param>
        /// <returns>True if the object is a group; otherwise, false.</returns>
        static bool IsGroup(string dn)
        {
            try
            {
                using (DirectoryEntry entry = new DirectoryEntry(GetLdapPath(dn)))
                {
                    // The "objectClass" property may have multiple values.
                    foreach (object o in entry.Properties["objectClass"])
                    {
                        if (o.ToString().Equals("group", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking object type for {0}: {1}", dn, ex.Message);
            }
            return false;
        }
    }
}