using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartKnowledgeBot.Business.Interfaces;
using SmartKnowledgeBot.Business.Services;
using SmartKnowledgeBot.Infrastructure.Data;
using SmartKnowledgeBot.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURATION =====
var configuration = builder.Configuration;
var jwtSettings = configuration.GetSection("JwtSettings");

// ===== DATABASE CONFIGURATION =====
builder.Services.AddDbContext<SmartKnowledgeBotDbContext>(options =>
{
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ===== AUTHENTICATION & AUTHORIZATION =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)),
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    // Configure JWT events for logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst("userId")?.Value;
            logger.LogDebug("JWT token validated for user: {UserId}", userId);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Define role-based policies
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DocumentManager", policy => policy.RequireRole("Admin", "DocumentManager"));
    options.AddPolicy("HRAccess", policy => policy.RequireRole("Admin", "HR", "Manager"));
    options.AddPolicy("ITAccess", policy => policy.RequireRole("Admin", "IT", "Manager"));
    options.AddPolicy("FinanceAccess", policy => policy.RequireRole("Admin", "Finance", "Manager"));
});

// ===== DEPENDENCY INJECTION =====

// Business Services
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Infrastructure Services
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// ===== API CONFIGURATION =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ===== SWAGGER/OPENAPI CONFIGURATION =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Smart Enterprise Knowledge Bot API",
        Version = "v1",
        Description = "Enterprise knowledge management system with AI-powered document retrieval",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@company.com"
        }
    });

    // Configure JWT authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// ===== CORS CONFIGURATION =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ===== HEALTH CHECKS =====
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SmartKnowledgeBotDbContext>("database")
    .AddCheck("azure-openai", () =>
    {
        // Simple health check for Azure OpenAI configuration
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];

        return !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey)
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Azure OpenAI configured")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Azure OpenAI not configured");
    })
    .AddCheck("azure-blob-storage", () =>
    {
        // Simple health check for Azure Blob Storage configuration
        var connectionString = configuration.GetConnectionString("AzureBlobStorage");

        return !string.IsNullOrEmpty(connectionString)
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Azure Blob Storage configured")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Azure Blob Storage not configured");
    });

// ===== LOGGING CONFIGURATION =====
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();

    if (builder.Environment.IsProduction())
    {
        loggingBuilder.AddApplicationInsights();
    }
});

// ===== HTTP CLIENT CONFIGURATION =====
builder.Services.AddHttpClient();

// ===== MEMORY CACHE =====
builder.Services.AddMemoryCache();

var app = builder.Build();

// Conditional HTTPS redirection
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
else
{
    // Only redirect to HTTPS if certificate is trusted
    var httpsPort = builder.Configuration.GetValue<int?>("HttpsPort");
    if (httpsPort.HasValue)
    {
        app.UseHttpsRedirection();
    }
}

// ===== MIDDLEWARE PIPELINE =====

// Exception handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Knowledge Bot API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Security headers
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// CORS
app.UseCors("AllowedOrigins");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.UseHealthChecks("/health");

// Detailed health checks with JSON response
app.UseHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

// API Controllers
app.MapControllers();

// Global error handling endpoint
app.Map("/error", (HttpContext context) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogError("Unhandled exception occurred");

    return Results.Problem(
        title: "An error occurred",
        detail: "An unexpected error occurred while processing your request.",
        statusCode: 500
    );
});

// ===== DATABASE INITIALIZATION =====
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<SmartKnowledgeBotDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Checking database connection...");
        await context.Database.CanConnectAsync();
        logger.LogInformation("Database connection successful");

        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Failed to initialize database");

        if (app.Environment.IsDevelopment())
        {
            throw; // Re-throw in development to see the error
        }
    }
}

app.Logger.LogInformation("Smart Enterprise Knowledge Bot API started successfully");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Application URLs: {Urls}", string.Join(", ", app.Urls));

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }