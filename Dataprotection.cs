[Serializable]
public class ProtectedToken
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public DateTime CalculatedExpiryTime { get; set; }
}

public class TokenService
{
    private readonly IDataProtector _protector;

    public TokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("TokenProtection");
    }

    public string ProtectToken(ProtectedToken token)
    {
        var tokenString = JsonConvert.SerializeObject(token);
        return _protector.Protect(tokenString);
    }

    public ProtectedToken UnprotectToken(string protectedTokenString)
    {
        var tokenString = _protector.Unprotect(protectedTokenString);
        return JsonConvert.DeserializeObject<ProtectedToken>(tokenString);
    }
}
public class TokenFileService
{
    private readonly string _filePath;

    public TokenFileService(string userDirectoryPath)
    {
        _filePath = Path.Combine(userDirectoryPath, "token.dat");
    }

    public void SaveToken(string protectedToken)
    {
        File.WriteAllText(_filePath, protectedToken);
    }

    public string ReadToken()
    {
        return File.Exists(_filePath) ? File.ReadAllText(_filePath) : null;
    }
}


var dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(@"path-to-directory"));
var tokenService = new TokenService(dataProtectionProvider);
var tokenFileService = new TokenFileService(@"path-to-user-directory");

// Simulated token response
var token = new ProtectedToken
{
    AccessToken = "YourAccessTokenHere",
    ExpiresIn = 3600,
    CalculatedExpiryTime = DateTime.UtcNow.AddSeconds(3600)
};

// Protect and save token
var protectedToken = tokenService.ProtectToken(token);
tokenFileService.SaveToken(protectedToken);

// Read and unprotect token
var protectedTokenFromFile = tokenFileService.ReadToken();
var tokenFromFile = tokenService.UnprotectToken(protectedTokenFromFile);

// Check expiry
if (tokenFromFile.CalculatedExpiryTime <= DateTime.UtcNow)
{
    Console.WriteLine("Token has expired. Fetch a new one.");
}
else
{
    Console.WriteLine("Token is still valid.");
}

