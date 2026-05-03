using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AIBanking.Data;

/// <summary>
/// Design-time factory used exclusively by EF Core tooling (migrations, scaffolding).
/// Avoids depending on the application's DI container at design time.
/// </summary>
public sealed class BankingDbContextFactory : IDesignTimeDbContextFactory<BankingDbContext>
{
    public BankingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<BankingDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        return new BankingDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Runtime singleton factory — builds its own <see cref="DbContextOptions{T}"/> from a
/// connection string so it never depends on the scoped options registered by AddDbContext.
/// Registered in Program.cs as a singleton for use by all singleton services.
/// </summary>
public sealed class BankingDbContextSingletonFactory(DbContextOptions<BankingDbContext> options)
    : IDbContextFactory<BankingDbContext>
{
    public BankingDbContext CreateDbContext() => new(options);

    public Task<BankingDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new BankingDbContext(options));
}
