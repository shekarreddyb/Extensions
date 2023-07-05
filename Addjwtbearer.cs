public async Task<IEnumerable<SecurityKey>> GetIssuerSigningKeysAsync()
{
    var keys = new List<SecurityKey>();

    using (var httpClient = new HttpClient())
    {
        // TODO: Replace with your public keys endpoint
        var response = await httpClient.GetAsync("https://your-provider.com/keys-endpoint");

        if (response.IsSuccessStatusCode)
        {
            var jsonWebKeys = await response.Content.ReadFromJsonAsync<List<JsonWebKey>>();
            
            foreach (var jsonWebKey in jsonWebKeys)
            {
                if (jsonWebKey.Kty == "RSA" && jsonWebKey.E != null && jsonWebKey.N != null)
                {
                    var rsaParameters = new RSAParameters
                    {
                        Exponent = Base64UrlEncoder.DecodeBytes(jsonWebKey.E),
                        Modulus = Base64UrlEncoder.DecodeBytes(jsonWebKey.N)
                    };
                    var rsa = RSA.Create();
                    rsa.ImportParameters(rsaParameters);
                    keys.Add(new RsaSecurityKey(rsa));
                }
            }
        }
    }

    return keys;
}
