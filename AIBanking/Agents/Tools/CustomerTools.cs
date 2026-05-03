using System.ComponentModel;
using System.Text.Json;
using AIBanking.Data;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Agents.Tools;

/// <summary>Kernel tools for customer and bank account queries.</summary>
internal sealed class CustomerTools(BankingDbContext context)
{
    [Description("List all customers. Optionally search by partial full name.")]
    public async Task<string> GetCustomersAsync(
        [Description("Optional partial name to search for (case-insensitive)")] string? name = null)
    {
        var query = context.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(c => c.FullName.ToLower().Contains(name.ToLower()));

        var customers = await query.OrderBy(c => c.FullName).ToListAsync();

        var result = customers.Select(c => new
        {
            id               = c.Id,
            fullName         = c.FullName,
            dateOfBirth      = c.DateOfBirth,
            gender           = c.Gender,
            phoneNumber      = c.PhoneNumber,
            residenceAddress = c.ResidenceAddress,
            applicationId    = c.ApplicationId,
            createdAt        = c.CreatedAt
        });

        return JsonSerializer.Serialize(result);
    }

    [Description("Get customer details linked to a specific account opening application.")]
    public async Task<string> GetCustomerByApplicationAsync(
        [Description("The account application ID (GUID string)")] string applicationId)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return JsonSerializer.Serialize(new { error = "Invalid application ID format." });

        var customer = await context.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ApplicationId == id);

        if (customer is null)
            return JsonSerializer.Serialize(new { error = $"No customer found for application {applicationId}. The CreateCustomer process may not have been run yet." });

        // Also include the bank account if it exists
        var account = await context.BankAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ApplicationId == id);

        return JsonSerializer.Serialize(new
        {
            customer = new
            {
                id               = customer.Id,
                fullName         = customer.FullName,
                dateOfBirth      = customer.DateOfBirth,
                gender           = customer.Gender,
                phoneNumber      = customer.PhoneNumber,
                residenceAddress = customer.ResidenceAddress,
                createdAt        = customer.CreatedAt
            },
            bankAccount = account is null ? null : new
            {
                id            = account.Id,
                accountNumber = account.AccountNumber,
                accountType   = account.AccountType.ToString(),
                createdAt     = account.CreatedAt
            }
        });
    }
}
