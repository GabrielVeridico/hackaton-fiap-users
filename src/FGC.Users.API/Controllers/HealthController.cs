using Microsoft.AspNetCore.Mvc;

namespace FGC.Users.API.Controllers;

[ApiController]
[Route("/")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy" });

    [HttpGet("ready")]
    public IActionResult Ready() => Ok(new { status = "ready" });
}
