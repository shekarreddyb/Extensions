using OfficeOpenXml;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        string filePath = @"path_to_your_excel_file.xlsx";
        string sheetName = "Sheet1"; // Replace with the actual sheet name

        // Load the Excel file
        FileInfo fileInfo = new FileInfo(filePath);
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage(fileInfo))
        {
            var worksheet = package.Workbook.Worksheets[sheetName];

            if (worksheet == null)
            {
                Console.WriteLine($"Sheet {sheetName} not found!");
                return;
            }

            // Find columns
            int appnameCol = 1;  // Replace with your actual column index if needed
            int vanityurlCol = 3; // Replace with your actual column index if needed
            int existsindnsCol = worksheet.Dimension.End.Column + 1; // Add a new column

            worksheet.Cells[1, existsindnsCol].Value = "existsindns"; // Add header

            int rowCount = worksheet.Dimension.End.Row;

            // Use a thread-safe collection to store results
            var results = new ConcurrentDictionary<int, string>();

            // Collect URLs and their row indices
            var urls = new ConcurrentBag<(int Row, string Url)>();
            for (int row = 2; row <= rowCount; row++) // Start from row 2 to skip headers
            {
                string url = worksheet.Cells[row, vanityurlCol].Text;
                if (!string.IsNullOrEmpty(url))
                {
                    urls.Add((row, url));
                }
                else
                {
                    results[row] = "no";
                }
            }

            // Process URLs in parallel
            Parallel.ForEach(urls, (item) =>
            {
                try
                {
                    Uri uri = new Uri(item.Url);
                    string host = uri.Host;

                    // Check DNS record
                    Dns.GetHostEntry(host);
                    results[item.Row] = "yes";
                }
                catch
                {
                    results[item.Row] = "no";
                }
            });

            // Update the worksheet with results
            foreach (var result in results)
            {
                worksheet.Cells[result.Key, existsindnsCol].Value = result.Value;
            }

            // Save the updated Excel file
            package.Save();
        }

        Console.WriteLine("Excel file updated successfully!");
    }
}