using System.ComponentModel;
using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using AIBanking.Models;
using AIBanking.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AIBanking.AIAgent.Agents.Tools;

/// <summary>
/// Action tools that let the agent review documents and drive an application
/// through its lifecycle: status transitions and service process execution.
/// </summary>
internal sealed class ApplicationActionTools(
    BankingDbContext          context,
    INotificationService      notifications,
    IDigitalEnrollmentService enrollmentService,
    ILogger                   logger)
{
    // ── Document review ─────────────────────────────────────────────────────

    [Description(
        "Review the uploaded documents and AI-extracted personal information for an application. " +
        "Returns document types present, what is still missing, and all extracted fields " +
        "(FullName, DateOfBirth, Gender, PhoneNumber, ResidenceAddress).")]
    public async Task<string> ReviewApplicationDocumentsAsync(
        [Description("The application ID (GUID string)")] string applicationId)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        var app = await context.AccountApplications
            .Include(a => a.Documents)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app is null) return Err($"Application {applicationId} not found.");

        var uploaded = app.Documents.Select(d => d.Type).ToHashSet();
        var missing  = Enum.GetValues<DocumentType>().Where(t => !uploaded.Contains(t)).ToList();

        return JsonSerializer.Serialize(new
        {
            applicationId = app.Id,
            status        = app.Status.ToString(),
            documents     = app.Documents.Select(d => new
            {
                type        = d.Type.ToString(),
                d.FileName,
                d.ContentType,
                d.UploadedAt
            }),
            missingDocuments = missing.Select(t => t.ToString()),
            allDocumentsUploaded = missing.Count == 0,
            extractedInfo = app.ExtractedInfo is null ? null : new
            {
                app.ExtractedInfo.FullName,
                app.ExtractedInfo.DateOfBirth,
                app.ExtractedInfo.Gender,
                app.ExtractedInfo.PhoneNumber,
                app.ExtractedInfo.ResidenceAddress
            }
        });
    }

    // ── Standards check ──────────────────────────────────────────────────────

    [Description(
        "Check whether an application meets the minimum standards required to proceed. " +
        "Verifies: both documents uploaded, FullName extracted, and at least 3 of 5 personal fields present. " +
        "Returns a pass/fail result with a list of specific issues found. " +
        "Always run this before approving an application or after documents are uploaded.")]
    public async Task<string> CheckApplicationStandardsAsync(
        [Description("The application ID (GUID string)")] string applicationId)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        var app = await context.AccountApplications
            .Include(a => a.Documents)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app is null) return Err($"Application {applicationId} not found.");

        var issues = new List<string>();

        // Document completeness
        var uploaded = app.Documents.Select(d => d.Type).ToHashSet();
        foreach (var required in Enum.GetValues<DocumentType>())
        {
            if (!uploaded.Contains(required))
                issues.Add($"Missing required document: {required}");
        }

        // Extraction completeness
        var info = app.ExtractedInfo;
        if (info is null)
        {
            issues.Add("No personal information has been extracted from the documents yet.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(info.FullName))
                issues.Add("FullName is missing — this is required.");

            var optional = new[] { info.DateOfBirth, info.Gender, info.PhoneNumber, info.ResidenceAddress };
            var missingOptional = optional.Count(string.IsNullOrWhiteSpace);
            if (missingOptional >= 3)
                issues.Add($"Only {4 - missingOptional} of 4 optional fields (DOB, Gender, Phone, Address) were extracted — at least 2 are required.");
        }

        return JsonSerializer.Serialize(new
        {
            applicationId = app.Id,
            status        = app.Status.ToString(),
            passes        = issues.Count == 0,
            issues,
            recommendation = issues.Count == 0
                ? "Application meets standards — safe to approve."
                : "Application does not meet standards — flag for rework and list the issues found."
        });
    }

    // ── Rework ───────────────────────────────────────────────────────────────

    [Description(
        "Flag an application for human rework when it fails to meet standards or a service process cannot proceed. " +
        "Sets the application status to Rework, records the specific issues in ReworkNotes, and resets any " +
        "Failed service processes back to Pending so they can be retried after the applicant makes corrections. " +
        "Use this instead of Rejecting when the problems are fixable (missing documents, incomplete info, data errors).")]
    public async Task<string> FlagApplicationForReworkAsync(
        [Description("The application ID (GUID string)")] string applicationId,
        [Description("Bullet-point list of specific issues found (e.g. 'Missing IdentityCard', 'PhoneNumber not extracted')")] string issues)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        if (string.IsNullOrWhiteSpace(issues))
            return Err("issues must describe what the applicant needs to fix.");

        var app = await context.AccountApplications
            .Include(a => a.Processes)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app is null) return Err($"Application {applicationId} not found.");

        if (app.Status == AccountStatus.Active)
            return Err("Cannot flag an Active application for rework.");

        var previous     = app.Status;
        app.Status       = AccountStatus.Rework;
        app.ReworkNotes  = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC]\n{issues}";
        app.UpdatedAt    = DateTime.UtcNow;

        // Reset any Failed processes so they can be retried after corrections
        var resetProcesses = new List<string>();
        foreach (var p in app.Processes.Where(p => p.Status == ServiceProcessStatus.Failed))
        {
            p.Status = ServiceProcessStatus.Pending;
            p.Error  = null;
            resetProcesses.Add(p.Name.ToString());
        }

        await context.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            applicationId  = app.Id,
            previousStatus = previous.ToString(),
            newStatus      = app.Status.ToString(),
            reworkNotes    = app.ReworkNotes,
            resetProcesses,
            updatedAt      = app.UpdatedAt,
            message        = "Application flagged for rework. The applicant or staff must correct the listed issues before processing can continue."
        });
    }

    // ── Status transition ────────────────────────────────────────────────────

    [Description(
        "Advance or update the status of an account opening application. " +
        "Valid transitions: Draft → PendingDocuments → UnderReview → Approved → Active, or any status → Rejected. " +
        "Use 'Approved' to green-light an application so service processes can run. " +
        "Use FlagApplicationForReworkAsync instead of this tool when sending back for corrections.")]
    public async Task<string> UpdateApplicationStatusAsync(
        [Description("The application ID (GUID string)")] string applicationId,
        [Description("New status: Draft, PendingDocuments, UnderReview, Approved, Active, Rejected")] string newStatus)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        if (!Enum.TryParse<AccountStatus>(newStatus, ignoreCase: true, out var status))
            return Err($"Unknown status '{newStatus}'. Valid values: {string.Join(", ", Enum.GetNames<AccountStatus>())}");

        var app = await context.AccountApplications.FindAsync(id);
        if (app is null) return Err($"Application {applicationId} not found.");

        var previous  = app.Status;
        app.Status    = status;
        app.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            applicationId = app.Id,
            previousStatus = previous.ToString(),
            newStatus      = app.Status.ToString(),
            updatedAt      = app.UpdatedAt
        });
    }

    // ── Service process: CreateCustomer ──────────────────────────────────────

    [Description(
        "Execute the CreateCustomer service process for an application. " +
        "Reads the AI-extracted personal information and creates a Customer record. " +
        "The application must be in Approved status and must have extracted info available. " +
        "Marks the CreateCustomer process as Completed on success.")]
    public async Task<string> RunCreateCustomerAsync(
        [Description("The application ID (GUID string)")] string applicationId)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        var app = await context.AccountApplications
            .Include(a => a.Processes)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app is null) return Err($"Application {applicationId} not found.");
        if (app.Status != AccountStatus.Approved)
            return Err($"Application must be in Approved status to run service processes (current: {app.Status}).");

        var process = app.GetProcess(ServiceProcess.CreateCustomer);
        if (process is null) return Err("CreateCustomer process entry not found on this application.");
        if (process.Status == ServiceProcessStatus.Completed)
            return Err($"CreateCustomer already completed. CustomerId = {process.ResultId}");

        if (app.ExtractedInfo is null || string.IsNullOrWhiteSpace(app.ExtractedInfo.FullName))
            return Err("No extracted person information available. Ensure documents have been uploaded and processed.");

        // Check whether a customer record already exists for this application
        var existing = await context.Customers.FirstOrDefaultAsync(c => c.ApplicationId == id);
        if (existing is not null)
        {
            process.Status      = ServiceProcessStatus.Completed;
            process.ResultId    = existing.Id;
            process.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return JsonSerializer.Serialize(new { success = true, customerId = existing.Id, note = "Customer already existed; process marked complete." });
        }

        var kycTier = DetermineKycTier(app);

        var customer = new Customer
        {
            Id               = Guid.NewGuid(),
            ApplicationId    = app.Id,
            FullName         = app.ExtractedInfo.FullName!,
            DateOfBirth      = app.ExtractedInfo.DateOfBirth,
            Gender           = app.ExtractedInfo.Gender,
            PhoneNumber      = app.ExtractedInfo.PhoneNumber,
            ResidenceAddress = app.ExtractedInfo.ResidenceAddress,
            NationalIdNumber = app.ExtractedInfo.NationalIdNumber ?? app.NinNumber,
            BvnNumber        = app.BvnNumber,
            KycTier          = kycTier,
            CreatedAt        = DateTime.UtcNow
        };

        context.Customers.Add(customer);

        process.Status      = ServiceProcessStatus.Completed;
        process.ResultId    = customer.Id;
        process.CompletedAt = DateTime.UtcNow;
        process.Error       = null;
        app.UpdatedAt       = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            success    = true,
            customerId = customer.Id,
            fullName   = customer.FullName,
            processStatus = process.Status.ToString()
        });
    }

    // ── Service process: CreateAccount ───────────────────────────────────────

    [Description(
        "Execute the CreateAccount service process for an application. " +
        "Creates a BankAccount linked to the customer created in the CreateCustomer step. " +
        "CreateCustomer must be completed first. " +
        "Marks the CreateAccount process as Completed and sets the application to Active on success.")]
    public async Task<string> RunCreateAccountAsync(
        [Description("The application ID (GUID string)")] string applicationId,
        [Description("Account type: Savings, Current, or FixedDeposit")] string accountType = "Savings")
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        if (!Enum.TryParse<AccountType>(accountType, ignoreCase: true, out var type))
            return Err($"Unknown account type '{accountType}'. Valid: Savings, Current, FixedDeposit.");

        var app = await context.AccountApplications
            .Include(a => a.Processes)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app is null) return Err($"Application {applicationId} not found.");
        if (app.Status != AccountStatus.Approved)
            return Err($"Application must be in Approved status (current: {app.Status}).");

        if (!app.IsProcessComplete(ServiceProcess.CreateCustomer))
            return Err("CreateCustomer process must be completed before CreateAccount can run.");

        var customerProcess = app.GetProcess(ServiceProcess.CreateCustomer)!;
        var customer = await context.Customers.FindAsync(customerProcess.ResultId);
        if (customer is null) return Err("Customer record referenced by CreateCustomer process was not found.");

        var accountProcess = app.GetProcess(ServiceProcess.CreateAccount);
        if (accountProcess is null) return Err("CreateAccount process entry not found.");
        if (accountProcess.Status == ServiceProcessStatus.Completed)
            return Err($"CreateAccount already completed. AccountId = {accountProcess.ResultId}");

        var existing = await context.BankAccounts.FirstOrDefaultAsync(b => b.ApplicationId == id);
        if (existing is not null)
        {
            accountProcess.Status      = ServiceProcessStatus.Completed;
            accountProcess.ResultId    = existing.Id;
            accountProcess.CompletedAt = DateTime.UtcNow;
            app.Status    = AccountStatus.Active;
            app.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return JsonSerializer.Serialize(new { success = true, accountId = existing.Id, note = "Account already existed; process marked complete." });
        }

        var (singleLimit, dailyLimit, maxBalance) = GetTierLimits(customer.KycTier);

        var account = new BankAccount
        {
            Id                     = Guid.NewGuid(),
            AccountNumber          = GenerateAccountNumber(),
            CustomerId             = customer.Id,
            ApplicationId          = app.Id,
            AccountType            = type,
            KycTier                = customer.KycTier,
            SingleTransactionLimit = singleLimit,
            DailyTransactionLimit  = dailyLimit,
            MaximumBalance         = maxBalance,
            CreatedAt              = DateTime.UtcNow
        };

        context.BankAccounts.Add(account);

        accountProcess.Status      = ServiceProcessStatus.Completed;
        accountProcess.ResultId    = account.Id;
        accountProcess.CompletedAt = DateTime.UtcNow;
        accountProcess.Error       = null;

        app.Status    = AccountStatus.Active;
        app.UpdatedAt = DateTime.UtcNow;

        // Auto-initiate debit card request (Mastercard, BranchPickup by default)
        var cardRequest = new CardRequest
        {
            Id            = Guid.NewGuid(),
            AccountId     = account.Id,
            CustomerId    = customer.Id,
            ApplicationId = app.Id,
            CardType      = CardType.Mastercard,
            DeliveryMethod= CardDeliveryMethod.BranchPickup,
            Status        = CardRequestStatus.Pending,
            RequestedAt   = DateTime.UtcNow
        };
        context.CardRequests.Add(cardRequest);

        // Create notification preferences (SMS mandatory, using extracted phone)
        var existingPrefs = await context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.CustomerId == customer.Id);
        if (existingPrefs is null)
        {
            context.NotificationPreferences.Add(new NotificationPreference
            {
                Id          = Guid.NewGuid(),
                CustomerId  = customer.Id,
                SmsEnabled  = true,
                PhoneNumber = customer.PhoneNumber ?? string.Empty,
                EmailEnabled= false,
                PushEnabled = false,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        // Auto-enroll in Mobile Banking and Internet Banking
        var mobileEnrollment  = (object?)null;
        var internetEnrollment= (object?)null;

        try
        {
            var mobile = await enrollmentService.EnrollAsync(
                customer.Id, account.Id, DigitalServiceType.MobileBanking,
                customer.FullName, account.AccountNumber);
            mobileEnrollment = new
            {
                username   = mobile.Username,
                status     = mobile.Status.ToString(),
                enrolledAt = mobile.EnrolledAt
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Mobile banking enrollment failed for customer {Id} — will require manual enrollment.", customer.Id);
        }

        try
        {
            var internet = await enrollmentService.EnrollAsync(
                customer.Id, account.Id, DigitalServiceType.InternetBanking,
                customer.FullName, account.AccountNumber);
            internetEnrollment = new
            {
                username   = internet.Username,
                status     = internet.Status.ToString(),
                enrolledAt = internet.EnrolledAt
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Internet banking enrollment failed for customer {Id} — will require manual enrollment.", customer.Id);
        }

        // Send welcome notification
        if (!string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            await notifications.SendAsync(customer.Id,
                "Welcome to AIBanking — Account Opened",
                $"Dear {customer.FullName}, your {type} account ({account.AccountNumber}) has been opened successfully. " +
                $"Your {cardRequest.CardType} debit card is being processed and will be available for pickup at your branch. " +
                $"Login credentials for Mobile and Internet Banking have been sent separately.");
        }

        return JsonSerializer.Serialize(new
        {
            success             = true,
            accountId           = account.Id,
            accountNumber       = account.AccountNumber,
            accountType         = account.AccountType.ToString(),
            customerId          = account.CustomerId,
            appStatus           = app.Status.ToString(),
            processStatus       = accountProcess.Status.ToString(),
            cardRequestId       = cardRequest.Id,
            cardType            = cardRequest.CardType.ToString(),
            cardDelivery        = cardRequest.DeliveryMethod.ToString(),
            mobileBanking       = mobileEnrollment,
            internetBanking     = internetEnrollment,
            welcomeSmsSent      = !string.IsNullOrWhiteSpace(customer.PhoneNumber)
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string Err(string message) =>
        JsonSerializer.Serialize(new { error = message });

    private static string GenerateAccountNumber()
    {
        // Format: YYYYMMDD + 6 random digits
        var rng = Random.Shared;
        return $"{DateTime.UtcNow:yyyyMMdd}{rng.Next(100_000, 999_999)}";
    }

    /// <summary>
    /// CBN-compliant KYC tier limits (NGN).
    /// Tier 1: BVN/NIN + bio-data  — low limits
    /// Tier 2: Govt ID + address   — medium limits
    /// Tier 3: Full KYC            — high limits
    /// </summary>
    private static (decimal single, decimal daily, decimal maxBalance) GetTierLimits(KycTier tier) =>
        tier switch
        {
            KycTier.Tier1 => (50_000m,    300_000m,    300_000m),
            KycTier.Tier2 => (200_000m,  1_000_000m, 5_000_000m),
            KycTier.Tier3 => (10_000_000m, 50_000_000m, decimal.MaxValue),
            _             => (50_000m,    300_000m,    300_000m)
        };

    /// <summary>
    /// Auto-assigns the CBN KYC tier based on available verified data.
    /// Tier 3: NIN + BVN + full address verified
    /// Tier 2: government ID + address
    /// Tier 1: BVN/NIN + basic bio-data only
    /// </summary>
    private static KycTier DetermineKycTier(AccountApplication app)
    {
        var info = app.ExtractedInfo;
        if (info is null) return KycTier.Tier1;

        var hasNin     = !string.IsNullOrWhiteSpace(app.NinNumber) || !string.IsNullOrWhiteSpace(info.NationalIdNumber);
        var hasBvn     = !string.IsNullOrWhiteSpace(app.BvnNumber);
        var hasAddress = !string.IsNullOrWhiteSpace(info.ResidenceAddress);
        var hasAllBio  = !string.IsNullOrWhiteSpace(info.FullName)
                      && !string.IsNullOrWhiteSpace(info.DateOfBirth)
                      && !string.IsNullOrWhiteSpace(info.Gender)
                      && !string.IsNullOrWhiteSpace(info.PhoneNumber);

        if (hasNin && hasBvn && hasAddress && hasAllBio)
            return KycTier.Tier3;

        if ((hasNin || hasBvn) && hasAddress)
            return KycTier.Tier2;

        return KycTier.Tier1;
    }
}
