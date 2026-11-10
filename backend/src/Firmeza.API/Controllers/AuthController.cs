using Firmeza.Application.DTOs;
using Firmeza.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace Firmeza.API.Controllers;

[ApiController, Route("api/auth"), AllowAnonymous, EnableRateLimiting("auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")] public Task<AuthDto> Login(LoginInput input, CancellationToken ct) => auth.LoginAsync(input, ct);
    [HttpPost("register")] public Task<AuthDto> Register(RegisterInput input, CancellationToken ct) => auth.RegisterAsync(input, ct);
}
