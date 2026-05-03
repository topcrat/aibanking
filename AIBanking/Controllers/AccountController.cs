using AIBanking.Data;
using AIBanking.DTOs;
using AIBanking.Enums;
using AIBanking.Models;
using AIBanking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Controllers;

[ApiController]
[Route("api/account/applications")]
public class AccountController : ControllerBase
{
    private readonly BankingDbContext          _context;
    private readonly IDocumentExtractionService _extractor;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        BankingDbContext           context,
        IDocumentExtractionService extractor,
        ILogger<AccountController> logger)
    {
        _context   = context;
        _extractor = extractor;
        _logger    = logger;
    }

    // ── Application lifecycle ──────────────────────────────────────────────

    /// <summary>Start a new account opening application.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var app = AccountApplication.Create();
        _context.AccountApplications.Add(app);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Account application {Id} created.", app.Id);
        return CreatedAtAction(nameof(GetById), new { id = app.Id }, ToResponse(app));
    }

    /// <summary>List all applications, optionally filtered by status.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AccountStatus? status = null, CancellationToken ct = default)
    {
        var query = _context.AccountApplications
            .Include(a => a.Documents)
            .Include(a => a.Processes)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var apps = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        return Ok(apps.Select(ToResponse));
    }

    /// <summary>Get a single application.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var app = await FindApplicationAsync(id, ct);
        if (app is null)
            return NotFound(new { Message = $"Application {id} not found." });

        return Ok(ToResponse(app));
    }

    // ── Document upload ────────────────────────────────────────────────────

    /// <summary>
    /// Upload an Account Opening Form or Identity Card.
    /// Accepted formats: JPEG, PNG, WebP, PDF.
    /// </summary>
    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> UploadDocument(
        Guid              id,
        IFormFile         file,
        [FromForm] DocumentType documentType,
        CancellationToken ct)
    {
        var app = await FindApplicationAsync(id, ct);
        if (app is null)
            return NotFound(new { Message = $"Application {id} not found." });

        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "No file provided." });

        if (!IsAcceptedContentType(file.ContentType))
            return BadRequest(new { Message = $"Unsupported file type '{file.ContentType}'. Use JPEG, PNG, WebP, or PDF." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        // Remove any previous upload of the same type then add the new one
        var existing = app.Documents.FirstOrDefault(d => d.Type == documentType);
        if (existing is not null)
            _context.ApplicationDocuments.Remove(existing);

        var doc = new ApplicationDocument
        {
            Id            = Guid.NewGuid(),
            ApplicationId = id,
            Type          = documentType,
            FileName      = Path.GetFileName(file.FileName),
            ContentType   = file.ContentType,
            Content       = ms.ToArray(),
            UploadedAt    = DateTime.UtcNow
        };

        _context.ApplicationDocuments.Add(doc);
        app.Documents.Add(doc);
        app.Status    = app.AllDocumentsUploaded() ? AccountStatus.PendingDocuments : AccountStatus.Draft;
        app.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Document {DocType} uploaded for application {AppId}.", documentType, id);

        return Ok(new DocumentSummary
        {
            Id          = doc.Id,
            Type        = doc.Type,
            FileName    = doc.FileName,
            ContentType = doc.ContentType,
            UploadedAt  = doc.UploadedAt
        });
    }

    // ── AI extraction ──────────────────────────────────────────────────────

    /// <summary>
    /// Run AI extraction on all uploaded documents.
    /// Requires both Account Opening Form and Identity Card.
    /// </summary>
    [HttpPost("{id:guid}/extract")]
    public async Task<IActionResult> Extract(Guid id, CancellationToken ct)
    {
        var app = await FindApplicationAsync(id, ct);
        if (app is null)
            return NotFound(new { Message = $"Application {id} not found." });

        if (!app.AllDocumentsUploaded())
            return BadRequest(new
            {
                Message  = "Both the Account Opening Form and Identity Card must be uploaded before extraction.",
                Uploaded = app.Documents.Select(d => d.Type)
            });

        var results = new List<(DocumentType type, ExtractedPersonInfo info)>();
        foreach (var doc in app.Documents)
        {
            var info = await _extractor.ExtractAsync(doc.Content, doc.ContentType, doc.Type, ct);
            results.Add((doc.Type, info));
            _logger.LogInformation("Extraction complete for {DocType} on application {AppId}.", doc.Type, id);
        }

        app.ExtractedInfo = _extractor.Merge(results);
        app.Status        = AccountStatus.UnderReview;
        app.UpdatedAt     = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Application {Id} moved to UnderReview after extraction.", id);
        return Ok(ToResponse(app));
    }

    /// <summary>Return the extracted person information for an application.</summary>
    [HttpGet("{id:guid}/extracted-info")]
    public async Task<IActionResult> GetExtractedInfo(Guid id, CancellationToken ct)
    {
        var app = await _context.AccountApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (app is null)
            return NotFound(new { Message = $"Application {id} not found." });

        if (app.ExtractedInfo is null)
            return NotFound(new { Message = "No extracted information available. Run /extract first." });

        return Ok(app.ExtractedInfo);
    }

    // ── Service processes ──────────────────────────────────────────────────

    /// <summary>List all service processes and their current status.</summary>
    [HttpGet("{id:guid}/processes")]
    public async Task<IActionResult> GetProcesses(Guid id, CancellationToken ct)
    {
        var exists = await _context.AccountApplications.AnyAsync(a => a.Id == id, ct);
        if (!exists)
            return NotFound(new { Message = $"Application {id} not found." });

        var processes = await _context.ApplicationProcesses
            .AsNoTracking()
            .Where(p => p.ApplicationId == id)
            .ToListAsync(ct);

        return Ok(processes.Select(ToProcessSummary));
    }

    /// <summary>
    /// Process 1 — Create Customer.
    /// Builds a Customer record from extracted information.
    /// Requires /extract to have been run first.
    /// </summary>
    [HttpPost("{id:guid}/processes/create-customer")]
    public async Task<IActionResult> CreateCustomer(Guid id, CancellationToken ct)
    {
        var app = await FindApplicationAsync(id, ct);
        if (app is null)
            return NotFound(new { Message = $"Application {id} not found." });

        if (app.ExtractedInfo is null)
            return BadRequest(new { Message = "Extracted information is missing. Run /extract first." });

        var process = app.GetProcess(ServiceProcess.CreateCustomer)!;

        if (process.Status == ServiceProcessStatus.Completed)
        {
            var existing = await _context.Customers.FindAsync([process.ResultId!.Value], ct);
            return Ok(new { Message = "Customer already created.", Customer = ToCustomerResponse(existing!) });
        }

        if (string.IsNullOrWhiteSpace(app.ExtractedInfo.FullName))
            return BadRequest(new { Message = "FullName could not be extracted. Please verify the uploaded documents." });

        var customer = new Customer
        {
            Id               = Guid.NewGuid(),
            ApplicationId    = id,
            FullName         = app.ExtractedInfo.FullName,
            DateOfBirth      = app.ExtractedInfo.DateOfBirth,
            Gender           = app.ExtractedInfo.Gender,
            PhoneNumber      = app.ExtractedInfo.PhoneNumber,
            ResidenceAddress = app.ExtractedInfo.ResidenceAddress,
            CreatedAt        = DateTime.UtcNow
        };

        _context.Customers.Add(customer);

        process.Status      = ServiceProcessStatus.Completed;
        process.ResultId    = customer.Id;
        process.CompletedAt = DateTime.UtcNow;
        app.Status          = AccountStatus.Approved;
        app.UpdatedAt       = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Customer {CustomerId} created for application {AppId}.", customer.Id, id);
        return Ok(new { Message = "Customer created successfully.", Customer = ToCustomerResponse(customer) });
    }

    /// <summary>
    /// Process 2 — Create Account.
    /// Opens a bank account linked to the customer from Process 1.
    /// Requires CreateCustomer to be completed first.
    /// </summary>
    [HttpPost("{id:guid}/processes/create-account")]
    public async Task<IActionResult> CreateAccount(Guid id, [FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var app = await FindApplicationAsync(id, ct);
        if (app is null)
            return NotFound(new { Message = $"Application {id} not found." });

        if (!app.IsProcessComplete(ServiceProcess.CreateCustomer))
            return BadRequest(new { Message = "CreateCustomer must be completed before CreateAccount." });

        var accountProcess = app.GetProcess(ServiceProcess.CreateAccount)!;

        if (accountProcess.Status == ServiceProcessStatus.Completed)
        {
            var existing = await _context.BankAccounts.FindAsync([accountProcess.ResultId!.Value], ct);
            return Ok(new { Message = "Account already created.", Account = ToBankAccountResponse(existing!) });
        }

        var customerId = app.GetProcess(ServiceProcess.CreateCustomer)!.ResultId!.Value;

        var account = new BankAccount
        {
            Id            = Guid.NewGuid(),
            AccountNumber = GenerateAccountNumber(),
            CustomerId    = customerId,
            ApplicationId = id,
            AccountType   = request.AccountType,
            CreatedAt     = DateTime.UtcNow
        };

        _context.BankAccounts.Add(account);

        accountProcess.Status      = ServiceProcessStatus.Completed;
        accountProcess.ResultId    = account.Id;
        accountProcess.CompletedAt = DateTime.UtcNow;

        if (app.AllProcessesComplete())
        {
            app.Status = AccountStatus.Active;
            _logger.LogInformation("Application {AppId} is now Active — all processes complete.", id);
        }

        app.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Bank account {AccountNumber} created for customer {CustomerId}.",
            account.AccountNumber, customerId);

        return Ok(new { Message = "Bank account created successfully.", Account = ToBankAccountResponse(account) });
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private Task<AccountApplication?> FindApplicationAsync(Guid id, CancellationToken ct) =>
        _context.AccountApplications
            .Include(a => a.Documents)
            .Include(a => a.Processes)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    private static string GenerateAccountNumber() =>
        $"BNK{Random.Shared.NextInt64(1_000_000_000L, 9_999_999_999L)}";

    private static bool IsAcceptedContentType(string contentType) =>
        contentType is "image/jpeg" or "image/png" or "image/webp" or "application/pdf";

    private static ProcessSummary ToProcessSummary(ApplicationProcess p) => new()
    {
        Name        = p.Name,
        Status      = p.Status,
        ResultId    = p.ResultId,
        CompletedAt = p.CompletedAt,
        Error       = p.Error
    };

    private static CustomerResponse ToCustomerResponse(Customer c) => new()
    {
        Id               = c.Id,
        ApplicationId    = c.ApplicationId,
        FullName         = c.FullName,
        DateOfBirth      = c.DateOfBirth,
        Gender           = c.Gender,
        PhoneNumber      = c.PhoneNumber,
        ResidenceAddress = c.ResidenceAddress,
        CreatedAt        = c.CreatedAt
    };

    private static BankAccountResponse ToBankAccountResponse(BankAccount a) => new()
    {
        Id            = a.Id,
        AccountNumber = a.AccountNumber,
        CustomerId    = a.CustomerId,
        ApplicationId = a.ApplicationId,
        AccountType   = a.AccountType,
        CreatedAt     = a.CreatedAt
    };

    private static AccountApplicationResponse ToResponse(AccountApplication app) => new()
    {
        Id            = app.Id,
        Status        = app.Status,
        ExtractedInfo = app.ExtractedInfo,
        Documents     = app.Documents.Select(d => new DocumentSummary
        {
            Id          = d.Id,
            Type        = d.Type,
            FileName    = d.FileName,
            ContentType = d.ContentType,
            UploadedAt  = d.UploadedAt
        }).ToList(),
        Processes    = app.Processes.Select(ToProcessSummary).ToList(),
        BvnNumber    = app.BvnNumber,
        NinNumber    = app.NinNumber,
        ConsentGiven = app.ConsentGiven,
        ReworkNotes  = app.ReworkNotes,
        CreatedAt    = app.CreatedAt,
        UpdatedAt    = app.UpdatedAt
    };
}
