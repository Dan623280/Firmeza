using Firmeza.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace Firmeza.Api.Controllers;

[ApiController, Route("api/v1/auth"), AllowAnonymous, EnableRateLimiting("authentication")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")] public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct) => StatusCode(201, await service.RegisterAsync(request, ct));
    [HttpPost("login")] public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) => Ok(await service.LoginAsync(request, ct));
}
