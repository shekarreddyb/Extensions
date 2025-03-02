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