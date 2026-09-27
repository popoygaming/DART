using DART.API.Net.Data;
using DART.API.Net.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DART.API.Net.Services;

public class DevelopmentAdminSeeder
{
    private readonly DartDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DevelopmentAdminSeeder> _logger;

    public DevelopmentAdminSeeder(
        DartDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration,
        ILogger<DevelopmentAdminSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var hasAdmin = await _dbContext.Users.AnyAsync(u => u.Role == AppRoles.Admin, cancellationToken);
        if (hasAdmin)
        {
            return;
        }

        var username = _configuration["DevelopmentAdmin:Username"];
        var password = _configuration["DevelopmentAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Development admin seeding skipped because credentials are not configured.");
            return;
        }

        var user = new User
        {
            Username = username.Trim(),
            Role = AppRoles.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Development admin account seeded with username '{Username}'.", user.Username);
    }
}
