using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DART.API.Net.Data;
using DART.API.Net.DTOs;
using DART.API.Net.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DART.API.Net.Services;

public class AuthenticationService
{
    private readonly DartDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthenticationService(
        DartDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        var normalizedUsername = username.Trim();

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);

        if (user is null)
        {
            return AuthenticationResult.InvalidCredentials();
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return AuthenticationResult.InvalidCredentials();
        }

        if (!user.IsActive)
        {
            return AuthenticationResult.InactiveAccount();
        }

        var jwtSettings = _configuration.GetSection("Jwt");
        var key = jwtSettings["Key"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expiresMinutesValue = jwtSettings["ExpiresMinutes"];

        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JWT configuration is incomplete.");
        }

        var expiresMinutes = 120;
        if (int.TryParse(expiresMinutesValue, out var parsed) && parsed > 0)
        {
            expiresMinutes = parsed;
        }

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        var response = new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new AuthUserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role
            }
        };

        return AuthenticationResult.Success(response);
    }
}

public sealed class AuthenticationResult
{
    private AuthenticationResult(bool isSuccess, bool isInactive, LoginResponse? response)
    {
        IsSuccess = isSuccess;
        IsInactive = isInactive;
        Response = response;
    }

    public bool IsSuccess { get; }

    public bool IsInactive { get; }

    public LoginResponse? Response { get; }

    public static AuthenticationResult Success(LoginResponse response)
    {
        return new AuthenticationResult(true, false, response);
    }

    public static AuthenticationResult InvalidCredentials()
    {
        return new AuthenticationResult(false, false, null);
    }

    public static AuthenticationResult InactiveAccount()
    {
        return new AuthenticationResult(false, true, null);
    }
}
