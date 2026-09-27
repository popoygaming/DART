using System.Text.Json;
using System.Text;
using DART.API.Net.Data;
using DART.API.Net.Models;
using DART.API.Net.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

var dartConnectionString = builder.Configuration.GetConnectionString("DartDatabase")
    ?? throw new InvalidOperationException("Connection string 'DartDatabase' is not configured.");

builder.Services.AddDbContext<DartDbContext>(options =>
    options.UseSqlServer(dartConnectionString));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<DevelopmentAdminSeeder>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) || string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT configuration is incomplete. Configure Jwt:Key, Jwt:Issuer, and Jwt:Audience.");
}

var jwtSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = jwtSigningKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Use framework-provided OpenAPI generation and include bearer auth definition for Swagger UI authorize support.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        var bearerScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Enter JWT token value only (without 'Bearer ' prefix)."
        };
        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

        document.Security ??= new List<OpenApiSecurityRequirement>();
        var bearerReference = new OpenApiSecuritySchemeReference("Bearer", document, null!);

        document.Security.Add(new OpenApiSecurityRequirement
        {
            [bearerReference] = new List<string>()
        });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // Serve static files (Swagger UI html we added under wwwroot/swagger)
    app.UseStaticFiles();
    // Map framework OpenAPI endpoints
    app.MapOpenApi();
}
else
{
    // Use a generic error handler that returns ProblemDetails without exposing internals.
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var problem = new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError
            };
            await JsonSerializer.SerializeAsync(context.Response.Body, problem);
        });
    });
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentAdminSeeder>();
    await seeder.SeedAsync(CancellationToken.None);
}

app.Run();
