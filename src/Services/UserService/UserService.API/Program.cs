using System.Security.Claims;
using Common.Infrastructure.Authentication;
using Common.Infrastructure.HealthChecks;
using Common.Infrastructure.Logging;
using Common.Contracts.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.API.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Persistence;
using UserService.Infrastructure.Persistence.Repositories;
using UserService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.AddStructuredLogging();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(connectionString));

// JWT Settings Configuration
builder.Services.Configure<Common.Infrastructure.Authentication.JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

// Repositories and Services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// RabbitMQ with MassTransit
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });
    });
});

// Health Checks
builder.Services.AddCustomHealthChecks(connectionString);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "User Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    dbContext.Database.Migrate();
}

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapCustomHealthChecks();

// ============================================
// API Endpoints
// ============================================

// Register new user
app.MapPost("/api/users/register", async (
    [FromBody] RegisterRequest request,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IPublishEndpoint publishEndpoint) =>
{
    // Check if user already exists
    if (await userRepository.ExistsAsync(request.Email))
    {
        return Results.BadRequest(new { error = "User with this email already exists" });
    }

    // Create user
    var passwordHash = passwordHasher.HashPassword(request.Password);
    var user = User.Create(
        request.Email,
        request.FirstName,
        request.LastName,
        passwordHash,
        request.PhoneNumber,
        request.DateOfBirth,
        request.Country);

    await userRepository.AddAsync(user);

    // Publish user created event
    await publishEndpoint.Publish(new UserCreatedEvent
    {
        UserId = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        CreatedAt = user.CreatedAt
    });

    var response = new UserResponse
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        DateOfBirth = user.DateOfBirth,
        Country = user.Country,
        IsActive = user.IsActive,
        IsEmailVerified = user.IsEmailVerified,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };

    return Results.Created($"/api/users/{user.Id}", response);
})
.WithName("RegisterUser")
.WithTags("Authentication")
.Produces<UserResponse>(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

// Login
app.MapPost("/api/users/login", async (
    [FromBody] LoginRequest request,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) =>
{
    var user = await userRepository.GetByEmailAsync(request.Email);
    
    if (user == null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    if (!user.IsActive)
    {
        return Results.BadRequest(new { error = "Account is deactivated" });
    }

    user.RecordLogin();
    await userRepository.UpdateAsync(user);

    var token = tokenService.GenerateToken(user);

    var response = new LoginResponse
    {
        Token = token,
        User = new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            Country = user.Country,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        }
    };

    return Results.Ok(response);
})
.WithName("Login")
.WithTags("Authentication")
.Produces<LoginResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized);

// Get current user profile
app.MapGet("/api/users/me", async (
    ClaimsPrincipal user,
    IUserRepository userRepository) =>
{
    var userIdClaim = user.FindFirst("userId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var currentUser = await userRepository.GetByIdAsync(userId);
    if (currentUser == null)
    {
        return Results.NotFound();
    }

    var response = new UserResponse
    {
        Id = currentUser.Id,
        Email = currentUser.Email,
        FirstName = currentUser.FirstName,
        LastName = currentUser.LastName,
        PhoneNumber = currentUser.PhoneNumber,
        DateOfBirth = currentUser.DateOfBirth,
        Country = currentUser.Country,
        IsActive = currentUser.IsActive,
        IsEmailVerified = currentUser.IsEmailVerified,
        CreatedAt = currentUser.CreatedAt,
        LastLoginAt = currentUser.LastLoginAt
    };

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithTags("Users")
.Produces<UserResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized);

// Get user by ID
app.MapGet("/api/users/{id:guid}", async (
    Guid id,
    IUserRepository userRepository) =>
{
    var user = await userRepository.GetByIdAsync(id);
    if (user == null)
    {
        return Results.NotFound();
    }

    var response = new UserResponse
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        DateOfBirth = user.DateOfBirth,
        Country = user.Country,
        IsActive = user.IsActive,
        IsEmailVerified = user.IsEmailVerified,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetUserById")
.WithTags("Users")
.Produces<UserResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// Update profile
app.MapPut("/api/users/me", async (
    ClaimsPrincipal user,
    [FromBody] UpdateProfileRequest request,
    IUserRepository userRepository) =>
{
    var userIdClaim = user.FindFirst("userId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var currentUser = await userRepository.GetByIdAsync(userId);
    if (currentUser == null)
    {
        return Results.NotFound();
    }

    currentUser.UpdateProfile(
        request.FirstName,
        request.LastName,
        request.PhoneNumber,
        request.Country);

    await userRepository.UpdateAsync(currentUser);

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("UpdateProfile")
.WithTags("Users")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status401Unauthorized);

// Change password
app.MapPost("/api/users/change-password", async (
    ClaimsPrincipal user,
    [FromBody] ChangePasswordRequest request,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) =>
{
    var userIdClaim = user.FindFirst("userId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var currentUser = await userRepository.GetByIdAsync(userId);
    if (currentUser == null)
    {
        return Results.NotFound();
    }

    if (!passwordHasher.VerifyPassword(request.CurrentPassword, currentUser.PasswordHash))
    {
        return Results.BadRequest(new { error = "Current password is incorrect" });
    }

    var newPasswordHash = passwordHasher.HashPassword(request.NewPassword);
    currentUser.UpdatePassword(newPasswordHash);
    await userRepository.UpdateAsync(currentUser);

    return Results.NoContent();
})
.RequireAuthorization()
.WithName("ChangePassword")
.WithTags("Users")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status400BadRequest);

app.Run();