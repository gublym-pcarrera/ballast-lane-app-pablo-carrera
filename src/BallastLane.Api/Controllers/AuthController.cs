using BallastLane.Api.Security;
using BallastLane.Application.Accounts;
using BallastLane.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BallastLane.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAccountService accounts, ICurrentUser currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var created = await accounts.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await accounts.LoginAsync(request, cancellationToken));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Me(CancellationToken cancellationToken)
    {
        return Ok(await accounts.GetAsync(currentUser.UserId, cancellationToken));
    }
}
