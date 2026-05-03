using System.Text;
using System.Text.Json.Serialization;
using AIBanking.Agents;
using AIBanking.Data;
using AIBanking.Services;
using Anthropic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// CORS — allow Vite dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// PostgreSQL / Entity Framework Core
builder.Services.AddDbContext<BankingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Singleton IDbContextFactory — builds its own options so it never touches the scoped
// DbContextOptions registered by AddDbContext above. Used by all singleton services.
{
    var opts = new DbContextOptionsBuilder<BankingDbContext>()
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .Options;
    builder.Services.AddSingleton<IDbContextFactory<BankingDbContext>>(
        new BankingDbContextSingletonFactory(opts));
}

// Document extraction — uses Claude vision API
builder.Services.AddHttpClient("anthropic");
builder.Services.AddSingleton<IDocumentExtractionService, ClaudeDocumentExtractionService>();

// Alert & notification service (SMS mandatory, email + push optional)
builder.Services.AddSingleton<INotificationService, NotificationService>();

// Digital onboarding services (BVN/NIN verification, fraud detection, digital enrollment)
builder.Services.AddSingleton<IBvnVerificationService, BvnVerificationService>();
builder.Services.AddSingleton<INinVerificationService, NinVerificationService>();
builder.Services.AddSingleton<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddSingleton<IDigitalEnrollmentService, DigitalEnrollmentService>();

// User management
builder.Services.AddSingleton<IUserService, UserService>();

// Microsoft Agent Framework — Anthropic client + banking agent
builder.Services.AddSingleton<IAnthropicClient>(
    new AnthropicClient { ApiKey = builder.Configuration["Anthropic:ApiKey"]! });
builder.Services.AddSingleton<IBankingAgentService, BankingAgentService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// JWT Authentication Setup
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankingDbContext>();
    await db.Database.MigrateAsync();
}

// Seed admin user if the users table is empty
await app.Services.GetRequiredService<IUserService>()
    .SeedAdminIfEmptyAsync(
        app.Configuration["AdminDefaults:Password"] ?? "Admin@1234!");

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
