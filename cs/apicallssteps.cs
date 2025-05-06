using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using TechTalk.SpecFlow;
using Xunit;

[Binding]
public class ApiSteps
{
    private HttpClient _client;
    private HttpRequestMessage _request;
    private HttpResponseMessage _response;
    private JObject _responseJson;
    private readonly ScenarioContext _ctx;

    public ApiSteps(ScenarioContext ctx)
    {
        _ctx = ctx;
        _client = new HttpClient(); // or inject if preferred
    }

    [Given(@"I set the base URI to ""(.*)""")]
    public void GivenISetBaseUri(string baseUri)
    {
        _client.BaseAddress = new Uri(baseUri);
    }

    [Given(@"I prepare a (GET|POST|PUT|DELETE) request to ""(.*)""")]
    public void GivenIPrepareRequest(string method, string path)
    {
        _request = new HttpRequestMessage(new HttpMethod(method), SubstituteVariables(path));
    }

    [Given(@"I prepare a (POST|PUT) request to ""(.*)"" with body from ""(.*)""")]
    public void GivenIPrepareRequestWithBody(string method, string path, string filePath)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, filePath);
        var json = File.ReadAllText(fullPath);
        _request = new HttpRequestMessage(new HttpMethod(method), SubstituteVariables(path));
        _request.Content = new StringContent(json, Encoding.UTF8, "application/json");
    }

    [When(@"I send the request")]
    public async Task WhenISendRequest()
    {
        _response = await _client.SendAsync(_request);
        _ctx["statusCode"] = (int)_response.StatusCode;

        var content = await _response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(content) && _response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            _responseJson = JObject.Parse(content);
            _ctx["responseJson"] = _responseJson;
        }
    }

    [Then(@"the response status code should be (\d+)")]
    public void ThenStatusCodeShouldBe(int expected)
    {
        Assert.Equal(expected, (int)_response.StatusCode);
    }

    [Then(@"the response should contain JSON field ""(.*)""")]
    public void ThenJsonFieldShouldExist(string jsonPath)
    {
        Assert.NotNull(_responseJson);
        Assert.NotNull(_responseJson.SelectToken(jsonPath));
    }

    [Then(@"the response should have JSON field ""(.*)"" with value ""(.*)""")]
    public void ThenJsonFieldShouldHaveValue(string jsonPath, string expected)
    {
        var token = _responseJson?.SelectToken(jsonPath);
        Assert.NotNull(token);
        Assert.Equal(expected, token.ToString());
    }

    [Then(@"save response field ""(.*)"" as ""(.*)""")]
    public void ThenSaveFieldToContext(string jsonPath, string key)
    {
        var token = _responseJson?.SelectToken(jsonPath);
        Assert.NotNull(token);
        _ctx[key] = token.ToString();
    }

    [Then(@"the response array field ""(.*)"" should have (\d+) items?")]
    public void ThenArrayFieldShouldHaveCount(string jsonPath, int count)
    {
        var token = _responseJson?.SelectToken(jsonPath);
        Assert.True(token is JArray, $"Expected JSON array at '{jsonPath}'");
        Assert.Equal(count, ((JArray)token).Count);
    }

    [Then(@"the response field ""(.*)"" should equal saved value ""(.*)""")]
    public void ThenFieldShouldEqualContextValue(string jsonPath, string contextKey)
    {
        var actual = _responseJson?.SelectToken(jsonPath)?.ToString();
        var expected = _ctx[contextKey]?.ToString();
        Assert.Equal(expected, actual);
    }

    private string SubstituteVariables(string input)
    {
        var pattern = @"\{([^}]+)\}";
        return Regex.Replace(input, pattern, match =>
        {
            var key = match.Groups[1].Value;
            return _ctx.ContainsKey(key) ? _ctx[key].ToString() : match.Value;
        });
    }
}