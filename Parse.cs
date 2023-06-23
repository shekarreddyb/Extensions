using System;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main()
    {
        Console.WriteLine(ParseNumberFromString("2Gi")); // Returns 2
        Console.WriteLine(ParseNumberFromString("3Ti")); // Returns 3072, as 3*1024
    }

    public static decimal ParseNumberFromString(string value)
    {
        // Handle null, empty or whitespace strings
        if (string.IsNullOrWhiteSpace(value)) return 0;

        // Regular expressions to match a decimal followed by "Gi" or "Ti"
        Match giMatch = Regex.Match(value, @"^\d*\.?\d*(?=Gi)");
        Match tiMatch = Regex.Match(value, @"^\d*\.?\d*(?=Ti)");

        if (giMatch.Success)
        {
            return decimal.Parse(giMatch.Value);
        }
        else if (tiMatch.Success)
        {
            return decimal.Parse(tiMatch.Value) * 1024;
        }
        else
        {
            throw new FormatException($"Unable to parse '{value}'");
        }
    }
}
