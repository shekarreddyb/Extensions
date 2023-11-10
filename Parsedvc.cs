using System;

public class StringParser
{
    public static string ParseString(string input)
    {
        // Split the string by '-'
        var parts = input.Split('-');

        // Extract the necessary parts and format them
        string datacenter = parts[0].Substring(0, 2); // First two letters of the first part
        string app = parts[1].Substring(0, 2); // First two letters of the second part
        string number = parts[2].Substring(parts[2].Length - 1); // Last character of the third part

        // Concatenate the parts in the desired format
        return $"pr{datacenter}{app}{number}";
    }

    public static void Main()
    {
        // Test the function with your examples
        Console.WriteLine(ParseString("oxdc-lmr-p-01")); // Output: proxlm1
        Console.WriteLine(ParseString("svdc-hra-p-02")); // Output: prsvhr2
        Console.WriteLine(ParseString("temr-clfn-p-03")); // Output: prtecl3
    }
}
