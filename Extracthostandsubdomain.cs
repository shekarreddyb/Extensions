
using System;

public class Program
{
    public static void Main()
    {
        var result = ExtractDomainParts("https://abc.xyz.com");
        Console.WriteLine(result.Item1); // Outputs: abc.xyz.com
        Console.WriteLine(result.Item2); // Outputs: https://xyz.com
    }

    public static Tuple<string, string> ExtractDomainParts(string url)
    {
        var uri = new Uri(url);

        string host = uri.Host; // abc.xyz.com
        int firstDot = host.IndexOf('.');
        string secondPart = host.Substring(firstDot + 1); // xyz.com

        return new Tuple<string, string>(host, uri.Scheme + "://" + secondPath);
    }
}
