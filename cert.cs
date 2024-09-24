
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Path to the .pfx file and its password
        string pfxFilePath = @"path\to\your\client-cert.pfx";
        string pfxPassword = "your-pfx-password";

        // Load the client certificate from the PFX file
        var clientCertificate = new X509Certificate2(pfxFilePath, pfxPassword);

        // Create an HttpClientHandler and attach the certificate
        var handler = new HttpClientHandler
        {
            ClientCertificates = { clientCertificate },
            // Enable NTLM Authentication
            Credentials = new NetworkCredential("username", "password", "domain"),
            UseDefaultCredentials = false,
            PreAuthenticate = true,
            // Optionally, validate server certificate if necessary
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
            {
                // You can add custom validation logic here (e.g., checking the server cert)
                return true; // Return true to accept the server certificate
            }
        };

        // Create an HttpClient with the handler
        using (var client = new HttpClient(handler))
        {
            // Set headers (if necessary)
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                // Make the request
                var response = await client.GetAsync("https://api.example.com/protected-endpoint");

                // Check for success status code
                response.EnsureSuccessStatusCode();

                // Read the response content
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response: " + content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}

