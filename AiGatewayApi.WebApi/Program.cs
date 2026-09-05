using AiGatewayApi.Application;
using AiGatewayApi.Application.Common.Interfaces;
using AiGatewayApi.Infrastructure;
using AiGatewayApi.Infrastructure.Persistence;
using AiGatewayApi.WebApi.Middlewares;
using AiGatewayApi.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Gateway Enterprise API",
        Version = "v1",
        Description = "Cổng Quản Trị & Điều Phối Mô Hình AI / LLM Tập Trung Toàn Doanh Nghiệp"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Bearer token: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Nhập Gateway API Key vào header X-Api-Key",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    options.Authority = jwtSettings["Issuer"] ?? "https://auth.company.com";
    options.Audience = jwtSettings["Audience"] ?? "erp-ecosystem";
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

string[] allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200", "http://localhost:4300", "http://localhost:4400", "http://localhost:4500", "http://localhost:4600", "http://localhost:8000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        _ = policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

WebApplication app = builder.Build();

// Auto-migrate and seed initial providers, models, projects, keys & templates
await AiGatewayDatabaseBootstrap.InitializeDatabaseAsync(app.Services);

if (app.Environment.IsDevelopment() || true)
{
    _ = app.UseSwagger();
    _ = app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Gateway API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("CorsPolicy");
app.UseMiddleware<GatewayKeyAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "AiGatewayApi", timestamp = DateTime.UtcNow }));
app.MapGet("/api/health", () => Results.Ok(new { status = "Healthy", service = "AiGatewayApi", timestamp = DateTime.UtcNow }));
app.MapGet("/health/live", () => Results.Ok(new { status = "Live", service = "AiGatewayApi" }));
app.MapGet("/health/ready", async (ApplicationDbContext db) =>
{
    var dbOk = await db.Database.CanConnectAsync();
    var ready = dbOk || !app.Environment.IsProduction();
    return Results.Json(new
    {
        status = ready ? "Ready" : "Degraded",
        service = "AiGatewayApi",
        db = dbOk,
        timestamp = DateTime.UtcNow
    }, statusCode: ready ? 200 : 503);
});

app.MapControllers();

app.Run();
