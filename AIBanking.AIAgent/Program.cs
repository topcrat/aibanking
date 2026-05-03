using System.Text;
using AIBanking.AIAgent.Agents;
using AIBanking.Data;
using AIBanking.Services;
using Anthropic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Scoped DbContext for per-request use (BankingAgentService resolves via IServiceScopeFactory)
builder.Services.AddDbContext<BankingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Singleton IDbContextFactory for singleton services
{
    var opts = new DbContextOptionsBuilder<BankingDbContext>()
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .Options;
    builder.Services.AddSingleton<IDbContextFactory<BankingDbContext>>(
        new BankingDbContextSingletonFactory(opts));
}

builder.Services.AddHttpClient("anthropic");
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<IBvnVerificationService, BvnVerificationService>();
builder.Services.AddSingleton<INinVerificationService, NinVerificationService>();
builder.Services.AddSingleton<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddSingleton<IDigitalEnrollmentService, DigitalEnrollmentService>();

builder.Services.AddSingleton<IAnthropicClient>(
    new AnthropicClient { ApiKey = builder.Configuration["Anthropic:ApiKey"]! });
builder.Services.AddSingleton<IBankingAgentService, BankingAgentService>();

builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience            = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

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
