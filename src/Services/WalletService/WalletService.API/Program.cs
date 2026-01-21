using System.Security.Claims;
using Common.Infrastructure.Authentication;
using Common.Infrastructure.HealthChecks;
using Common.Infrastructure.Logging;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using WalletService.API.Consumers;
using WalletService.API.DTOs;
using WalletService.API.Services;
using WalletService.Domain.Repositories;
using WalletService.Infrastructure.Persistence;
using WalletService.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.AddStructuredLogging();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<WalletDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis connection string not found.");

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnection));

// JWT Settings Configuration
builder.Services.Configure<Common.Infrastructure.Authentication.JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

// Repositories and Services
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IWalletCacheService, WalletCacheService>();

// JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// RabbitMQ with MassTransit
builder.Services.AddMassTransit(x =>
{
    // Add consumers
    x.AddConsumer<UserCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        // Configure endpoints for consumers
        cfg.ConfigureEndpoints(context);
    });
});

// Health Checks
builder.Services.AddCustomHealthChecks(connectionString);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Wallet Service API", Version = "v1" });
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
    var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
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

// Get wallet for current user
app.MapGet("/api/wallets/me", async (
    ClaimsPrincipal user,
    IWalletRepository walletRepository,
    IWalletCacheService cacheService) =>
{
    var userIdClaim = user.FindFirst("userId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var wallet = await walletRepository.GetByUserIdAsync(userId);
    if (wallet == null)
    {
        return Results.NotFound(new { error = "Wallet not found" });
    }

    // Try to get balance from cache
    var cachedBalance = await cacheService.GetBalanceAsync(wallet.Id);
    if (cachedBalance.HasValue)
    {
        // Update wallet balance from cache (in case it's more current)
        var response = new WalletResponse
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = cachedBalance.Value,
            Currency = wallet.Currency,
            Status = wallet.Status.ToString(),
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.UpdatedAt
        };
        return Results.Ok(response);
    }

    // Cache the balance for future requests
    await cacheService.SetBalanceAsync(wallet.Id, wallet.Balance);

    var walletResponse = new WalletResponse
    {
        Id = wallet.Id,
        UserId = wallet.UserId,
        Balance = wallet.Balance,
        Currency = wallet.Currency,
        Status = wallet.Status.ToString(),
        CreatedAt = wallet.CreatedAt,
        UpdatedAt = wallet.UpdatedAt
    };

    return Results.Ok(walletResponse);
})
.RequireAuthorization()
.WithName("GetMyWallet")
.WithTags("Wallet")
.Produces<WalletResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound);

// Get wallet by ID (admin or authorized user)
app.MapGet("/api/wallets/{id:guid}", async (
    Guid id,
    ClaimsPrincipal user,
    IWalletRepository walletRepository,
    IWalletCacheService cacheService) =>
{
    var userIdClaim = user.FindFirst("userId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var wallet = await walletRepository.GetByIdAsync(id);
    if (wallet == null)
    {
        return Results.NotFound(new { error = "Wallet not found" });
    }

    // Verify the wallet belongs to the requesting user
    if (wallet.UserId != userId)
    {
        return Results.Forbid();
    }

    // Try to get balance from cache
    var cachedBalance = await cacheService.GetBalanceAsync(wallet.Id);

    var response = new WalletResponse
    {
        Id = wallet.Id,
        UserId = wallet.UserId,
        Balance = cachedBalance ?? wallet.Balance,
        Currency = wallet.Currency,
        Status = wallet.Status.ToString(),
        CreatedAt = wallet.CreatedAt,
        UpdatedAt = wallet.UpdatedAt
    };

    // Cache if not already cached
    if (!cachedBalance.HasValue)
    {
        await cacheService.SetBalanceAsync(wallet.Id, wallet.Balance);
    }

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetWalletById")
.WithTags("Wallet")
.Produces<WalletResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status404NotFound);

// Get wallet by user ID (internal use)
app.MapGet("/api/wallets/user/{userId:guid}", async (
    Guid userId,
    IWalletRepository walletRepository,
    IWalletCacheService cacheService) =>
{
    var wallet = await walletRepository.GetByUserIdAsync(userId);
    if (wallet == null)
    {
        return Results.NotFound(new { error = "Wallet not found for user" });
    }

    // Try to get balance from cache
    var cachedBalance = await cacheService.GetBalanceAsync(wallet.Id);

    var response = new WalletResponse
    {
        Id = wallet.Id,
        UserId = wallet.UserId,
        Balance = cachedBalance ?? wallet.Balance,
        Currency = wallet.Currency,
        Status = wallet.Status.ToString(),
        CreatedAt = wallet.CreatedAt,
        UpdatedAt = wallet.UpdatedAt
    };

    // Cache if not already cached
    if (!cachedBalance.HasValue)
    {
        await cacheService.SetBalanceAsync(wallet.Id, wallet.Balance);
    }

    return Results.Ok(response);
})
.WithName("GetWalletByUserId")
.WithTags("Wallet")
.Produces<WalletResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// Get transaction history with pagination
app.MapGet("/api/wallets/me/transactions", async (
    ClaimsPrincipal user,
    IWalletRepository walletRepository,
    ITransactionRepository transactionRepository,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20) =>
{
    var userIdClaim = user.FindFirst("userId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    // Validate pagination parameters
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 20;
    if (pageSize > 100) pageSize = 100; // Max page size

    var wallet = await walletRepository.GetByUserIdAsync(userId);
    if (wallet == null)
    {
        return Results.NotFound(new { error = "Wallet not found" });
    }

    // Get transactions and total count
    var transactions = await transactionRepository.GetByWalletIdAsync(wallet.Id, page, pageSize);
    var totalCount = await transactionRepository.GetCountByWalletIdAsync(wallet.Id);
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    var transactionResponses = transactions.Select(t => new TransactionResponse
    {
        Id = t.Id,
        WalletId = t.WalletId,
        Type = t.Type.ToString(),
        Amount = t.Amount,
        BalanceBefore = t.BalanceBefore,
        BalanceAfter = t.BalanceAfter,
        Currency = t.Currency,
        Description = t.Description,
        ReferenceId = t.ReferenceId,
        Status = t.Status.ToString(),
        CreatedAt = t.CreatedAt
    });

    var response = new TransactionHistoryResponse
    {
        Transactions = transactionResponses,
        CurrentPage = page,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = totalPages,
        HasPreviousPage = page > 1,
        HasNextPage = page < totalPages
    };

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetMyTransactionHistory")
.WithTags("Transactions")
.Produces<TransactionHistoryResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound);

// Get transaction history by wallet ID with pagination
app.MapGet("/api/wallets/{walletId:guid}/transactions", async (
    Guid walletId,
    ClaimsPrincipal user,
    IWalletRepository walletRepository,
    ITransactionRepository transactionRepository,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20) =>
{
    var userIdClaim = user.FindFirst("userId")?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    // Validate pagination parameters
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 20;
    if (pageSize > 100) pageSize = 100; // Max page size

    var wallet = await walletRepository.GetByIdAsync(walletId);
    if (wallet == null)
    {
        return Results.NotFound(new { error = "Wallet not found" });
    }

    // Verify the wallet belongs to the requesting user
    if (wallet.UserId != userId)
    {
        return Results.Forbid();
    }

    // Get transactions and total count
    var transactions = await transactionRepository.GetByWalletIdAsync(walletId, page, pageSize);
    var totalCount = await transactionRepository.GetCountByWalletIdAsync(walletId);
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

    var transactionResponses = transactions.Select(t => new TransactionResponse
    {
        Id = t.Id,
        WalletId = t.WalletId,
        Type = t.Type.ToString(),
        Amount = t.Amount,
        BalanceBefore = t.BalanceBefore,
        BalanceAfter = t.BalanceAfter,
        Currency = t.Currency,
        Description = t.Description,
        ReferenceId = t.ReferenceId,
        Status = t.Status.ToString(),
        CreatedAt = t.CreatedAt
    });

    var response = new TransactionHistoryResponse
    {
        Transactions = transactionResponses,
        CurrentPage = page,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = totalPages,
        HasPreviousPage = page > 1,
        HasNextPage = page < totalPages
    };

    return Results.Ok(response);
})
.RequireAuthorization()
.WithName("GetTransactionHistory")
.WithTags("Transactions")
.Produces<TransactionHistoryResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status403Forbidden)
.Produces(StatusCodes.Status404NotFound);

app.Run();
