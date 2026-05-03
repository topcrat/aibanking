using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AIBanking.Models;
using AIBanking.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AIBanking.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IUserService              userService,
    IConfiguration            configuration,
    ILogger<AuthController>   logger) : ControllerBase
{
    // ── Login ─────────────────────────────────────────────────────────────────

    /// <summary>Authenticate and receive a JWT token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        var user = await userService.ValidateAsync(request.Username, request.Password, ct);
        if (user is null)
        {
            logger.LogWarning("Failed login for '{Username}'.", request.Username);
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var token = GenerateJwtToken(user);
        logger.LogInformation("User '{Username}' authenticated.", user.Username);

        return Ok(new
        {
            token,
            message  = "Login successful",
            userId   = user.Id,
            username = user.Username,
            fullName = user.FullName,
            role     = user.Role
        });
    }

    // ── User management (Admin only) ──────────────────────────────────────────

    /// <summary>List all users. Admin only.</summary>
    [HttpGet("users")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var users = await userService.ListAsync(ct);
        return Ok(users.Select(ToResponse));
    }

    /// <summary>Create a new user. Admin only.</summary>
    [HttpPost("users")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { message = "Username is required." });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { message = "Password must be at least 8 characters." });
        if (!UserRoles.All.Contains(request.Role))
            return BadRequest(new { message = $"Invalid role. Valid roles: {string.Join(", ", UserRoles.All)}" });

        try
        {
            var user = await userService.CreateAsync(
                request.Username, request.Password, request.FullName, request.Role, ct);
            return CreatedAtAction(nameof(ListUsers), null, ToResponse(user));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>Change a user's password. Admin can change any; staff can only change their own.</summary>
    [HttpPut("users/{id:guid}/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        // Staff can only change their own password
        var callerRole = User.FindFirstValue(ClaimTypes.Role);
        var callerId   = Guid.TryParse(User.FindFirstValue("uid"), out var cid) ? cid : Guid.Empty;

        if (callerRole != UserRoles.Admin && callerId != id)
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest(new { message = "New password must be at least 8 characters." });

        var ok = await userService.ChangePasswordAsync(id, request.NewPassword, ct);
        if (!ok) return NotFound(new { message = $"User {id} not found." });

        return Ok(new { message = "Password updated successfully." });
    }

    /// <summary>Activate or deactivate a user. Admin only.</summary>
    [HttpPut("users/{id:guid}/active")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest request, CancellationToken ct)
    {
        var ok = await userService.SetActiveAsync(id, request.IsActive, ct);
        if (!ok) return NotFound(new { message = $"User {id} not found." });

        return Ok(new { message = $"User {(request.IsActive ? "activated" : "deactivated")}." });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GenerateJwtToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("uid",                          user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub,    user.Username),
                new Claim(JwtRegisteredClaimNames.Name,   user.FullName),
                new Claim(ClaimTypes.Role,                user.Role),
                new Claim(JwtRegisteredClaimNames.Jti,    Guid.NewGuid().ToString()),
            ]),
            Expires            = DateTime.UtcNow.AddHours(8),
            Issuer             = configuration["JwtSettings:Issuer"],
            Audience           = configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static object ToResponse(User u) => new
    {
        id          = u.Id,
        username    = u.Username,
        fullName    = u.FullName,
        role        = u.Role,
        isActive    = u.IsActive,
        createdAt   = u.CreatedAt,
        lastLoginAt = u.LastLoginAt
    };
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role     { get; set; } = UserRoles.Staff;
}

public sealed class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class SetActiveRequest
{
    public bool IsActive { get; set; }
}
