using Microsoft.AspNetCore.Mvc;
using CallbackViewer.Services;

namespace CallbackViewer.Controllers;

[ApiController]
[Route("api/callback")]
public class CallbackController : ControllerBase
{
    private readonly CallbackDataService _callbackDataService;

    public CallbackController(CallbackDataService callbackDataService)
    {
        _callbackDataService = callbackDataService;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveCallback()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        _callbackDataService.AddCallback(body);
        return Ok();
    }
}