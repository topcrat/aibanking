using System.Collections.Concurrent;
using System.Text.Json;
using AIBanking.Agents.Tools;
using AIBanking.Data;
using AIBanking.Services;
using Anthropic;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ChatRequest  = AIBanking.DTOs.ChatRequest;
using ChatResponse = AIBanking.DTOs.ChatResponse;

namespace AIBanking.Agents;

public sealed class BankingAgentService : IBankingAgentService
{
    private readonly IAnthropicClient          _anthropic;
    private readonly string                    _model;
    private readonly IServiceScopeFactory      _scopeFactory;
    private readonly ILoggerFactory            _loggerFactory;
    private readonly INotificationService      _notifications;
    private readonly IBvnVerificationService   _bvnService;
    private readonly INinVerificationService   _ninService;
    private readonly IFraudDetectionService    _fraudService;
    private readonly IDigitalEnrollmentService _enrollmentService;

    // Serialized sessions keyed by conversationId — survives across requests
    private readonly ConcurrentDictionary<string, JsonElement> _sessions = new();

    private const string SystemPrompt = """
        You are a professional AI banking operations assistant for AIBanking.

        ## What you can do

        ### Query (read-only)
        - List and filter account opening applications by status
        - Get full application details including documents and extracted person info
        - List and filter workflow approval items
        - Look up customers and their linked bank accounts
        - Get a workflow summary (counts by status)

        ### Document review
        - Review uploaded documents and AI-extracted fields (FullName, DOB, Gender, Phone, Address)
        - Identify which documents are still missing
        - Summarise whether extracted information is complete enough to proceed

        ### Application lifecycle actions
        - Check whether an application meets standards before approving
        - Update application status (Draft → PendingDocuments → UnderReview → Approved → Active / Rejected)
        - Flag an application for human rework when standards are not met (sets status to Rework, records issues)
        - Execute the CreateCustomer service process (creates a Customer record from extracted info)
        - Execute the CreateAccount service process (creates a BankAccount; requires CreateCustomer first)
        - Both service processes run only when the application is Approved

        ### Workflow review actions
        - Approve a workflow item (from Pending or Rework)
        - Request rework on a workflow item (from Pending) — always include what needs to be corrected
        - Decline a workflow item (from Pending or Rework) — always include the reason

        ### Debit card management
        - View card request details and list all card requests by status
        - Update card type (Verve / Mastercard) and delivery method (BranchPickup / HomeDelivery)
        - Advance card status: Pending → Processing → Dispatched (with tracking number) → Delivered
        - Trigger PIN generation — PIN is delivered via SMS only and never stored
        - Card request is auto-created when an account is opened (Mastercard, BranchPickup by default)

        ### Notification management
        - View a customer's notification preferences (SMS mandatory, email and push optional)
        - Update preferences: phone number, email, push device token, enable/disable channels
        - View notification history for a customer
        - Send a manual notification to a customer across all enabled channels

        ### BVN verification
        - View and check BVN (Bank Verification Number) status for an application
        - Verify a BVN against the national database (NIBSS) — must be done before approving
        - BVN must be 11 digits; status: Verified / Suspicious (data mismatch) / Failed (not found)
        - Flag for rework if BVN is Failed or Suspicious (applicant must provide a valid BVN)

        ### NIN verification
        - View and check NIN (National Identification Number) status for an application
        - Verify a NIN against the NIMC national database — required alongside BVN before approving
        - NIN must be 11 digits; status: Verified / Suspicious (data mismatch) / Failed (not found)
        - Flag for rework if NIN is Failed or Suspicious (applicant must provide a valid NIN)

        ### KYC tier
        - KYC tier (1, 2, or 3) is auto-assigned when the customer record is created
        - Tier 1: BVN/NIN + basic bio-data only — low transaction limits (₦50k single, ₦300k daily, ₦300k max balance)
        - Tier 2: Govt ID + verified address — medium limits (₦200k single, ₦1M daily, ₦5M max balance)
        - Tier 3: Full KYC (NIN + BVN + full address + all bio-data) — high limits (₦10M single, ₦50M daily)
        - Always report the assigned KYC tier when creating an account

        ### Consent & NDPA compliance
        - Applicants must give explicit data-usage consent (NDPA requirement) before account activation
        - Missing consent is flagged as a medium-weight fraud signal (10 points)
        - Confirm consent status when reviewing applications

        ### Fraud detection
        - Run rule-based fraud risk assessment (score 0–100, levels: Low/Medium/High/Critical)
        - Fraud rules include: BVN status, NIN status, duplicate BVN/phone, missing documents, PEP/sanctions screening, missing consent
        - Review triggered risk flags and record manual review outcome (Cleared/Confirmed/Escalated)
        - Low risk: proceed normally. Medium: review flags. High/Critical: escalate before approving
        - PEP match is a 50-point signal — always requires human review before approval

        ### Digital banking enrollment
        - Enroll customers in Mobile Banking and Internet Banking
        - Auto-enrolls when an account is opened; credentials sent via SMS only
        - Manual re-enrollment available if auto-enrollment failed
        - View enrollment status — usernames are visible, passwords are never shown

        ### Onboarding KPIs
        - Record onboarding completion timing after account opening
        - Target KPI: complete account opening within 3 minutes (180 seconds)
        - View aggregated KPI summary: success rates, average duration, % meeting 3-minute target

        ## Decision rules

        ### When to flag an application for rework (NOT reject)
        Flag for rework if the problem is fixable by the applicant or staff:
        - A required document (AccountOpeningForm or IdentityCard) is missing
        - FullName was not extracted from the documents
        - Fewer than 2 of the 4 optional fields (DOB, Gender, Phone, Address) were extracted
        - A service process (CreateCustomer or CreateAccount) returned an error due to missing data

        ### When to reject
        Reject only for policy violations or fraud signals (explicit human instruction required).

        ### Standard processing flow
        When asked to "process", "review", or "advance" an application:
        1. Call CheckApplicationStandardsAsync — get pass/fail with specific issues
        2. If FAILS → call FlagApplicationForReworkAsync with the exact issues — STOP
        3. If PASSES → run VerifyBvnAsync (using the application's BvnNumber + extracted name/DOB)
           - If BVN Failed/Suspicious → FlagApplicationForReworkAsync — STOP
        3b. Run VerifyNinAsync (using the application's NinNumber or extracted NationalIdNumber + name/DOB)
           - If NIN Failed/Suspicious → FlagApplicationForReworkAsync — STOP
        4. Run RunFraudAssessmentAsync
           - Low risk → proceed
           - Medium risk → proceed with caution (note the flags in your summary)
           - High/Critical → inform the human, do NOT auto-approve without explicit instruction
           - PEP match → always escalate; do NOT approve without explicit human instruction
        5. Advance status to Approved → RunCreateCustomerAsync (auto-assigns KYC tier) → RunCreateAccountAsync
           (RunCreateAccountAsync auto-enrolls Mobile and Internet Banking; credentials sent via SMS)
        6. Call RecordOnboardingMetricAsync: lastStage="AccountCreated", isSuccess=true, outcome="AccountOpened"
        7. On any service process error → FlagApplicationForReworkAsync; RecordOnboardingMetricAsync with isSuccess=false
        8. Always report the final state, assigned KYC tier, transaction limits, and what the human needs to do next.

        ### Workflow review flow
        Before making any approval decision on a workflow item, always fetch its current details first.
        Provide the reviewer name and clear comments with every decision.

        ## General rules
        - Never fabricate data — use tools to fetch real information.
        - Be concise and professional.
        - After every action, summarise what changed and what the next step is.
        """;

    public BankingAgentService(
        IAnthropicClient          anthropic,
        IConfiguration            configuration,
        IServiceScopeFactory      scopeFactory,
        ILoggerFactory            loggerFactory,
        INotificationService      notifications,
        IBvnVerificationService   bvnService,
        INinVerificationService   ninService,
        IFraudDetectionService    fraudService,
        IDigitalEnrollmentService enrollmentService)
    {
        _anthropic         = anthropic;
        _model             = configuration["Anthropic:DeploymentName"] ?? "claude-opus-4-6";
        _scopeFactory      = scopeFactory;
        _loggerFactory     = loggerFactory;
        _notifications     = notifications;
        _bvnService        = bvnService;
        _ninService        = ninService;
        _fraudService      = fraudService;
        _enrollmentService = enrollmentService;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        // Create a DI scope so we can resolve the scoped BankingDbContext
        using var scope   = _scopeFactory.CreateScope();
        var       context = scope.ServiceProvider.GetRequiredService<BankingDbContext>();

        // Build tools backed by the scoped DbContext
        var appLogger      = _loggerFactory.CreateLogger<ApplicationActionTools>();
        var accountTools   = new AccountTools(context);
        var workflowTools  = new WorkflowTools(context);
        var customerTools  = new CustomerTools(context);
        var appActions     = new ApplicationActionTools(context, _notifications, _enrollmentService, appLogger);
        var wfActions      = new WorkflowActionTools(context);
        var cardTools      = new CardTools(context);
        var cardActions    = new CardActionTools(context, _notifications);
        var complianceTools= new ComplianceTools(context);
        var complianceActions = new ComplianceActionTools(context, _bvnService, _ninService, _fraudService, _enrollmentService);

        IList<AITool> tools =
        [
            // ── Query tools ──────────────────────────────────────────────
            AIFunctionFactory.Create(accountTools.GetApplicationsAsync),
            AIFunctionFactory.Create(accountTools.GetApplicationStatusAsync),
            AIFunctionFactory.Create(workflowTools.GetWorkflowsAsync),
            AIFunctionFactory.Create(workflowTools.GetWorkflowStatusAsync),
            AIFunctionFactory.Create(workflowTools.GetWorkflowSummaryAsync),
            AIFunctionFactory.Create(customerTools.GetCustomersAsync),
            AIFunctionFactory.Create(customerTools.GetCustomerByApplicationAsync),

            // ── Application action tools ─────────────────────────────────
            AIFunctionFactory.Create(appActions.ReviewApplicationDocumentsAsync),
            AIFunctionFactory.Create(appActions.CheckApplicationStandardsAsync),
            AIFunctionFactory.Create(appActions.FlagApplicationForReworkAsync),
            AIFunctionFactory.Create(appActions.UpdateApplicationStatusAsync),
            AIFunctionFactory.Create(appActions.RunCreateCustomerAsync),
            AIFunctionFactory.Create(appActions.RunCreateAccountAsync),

            // ── Workflow action tools ────────────────────────────────────
            AIFunctionFactory.Create(wfActions.ApproveWorkflowAsync),
            AIFunctionFactory.Create(wfActions.RequestWorkflowReworkAsync),
            AIFunctionFactory.Create(wfActions.DeclineWorkflowAsync),

            // ── Card & notification query tools ──────────────────────────
            AIFunctionFactory.Create(cardTools.GetCardRequestAsync),
            AIFunctionFactory.Create(cardTools.GetCardRequestsAsync),
            AIFunctionFactory.Create(cardTools.GetNotificationHistoryAsync),
            AIFunctionFactory.Create(cardTools.GetNotificationPreferencesAsync),

            // ── Card & notification action tools ─────────────────────────
            AIFunctionFactory.Create(cardActions.UpdateCardRequestAsync),
            AIFunctionFactory.Create(cardActions.UpdateCardStatusAsync),
            AIFunctionFactory.Create(cardActions.GenerateCardPinAsync),
            AIFunctionFactory.Create(cardActions.UpdateNotificationPreferencesAsync),
            AIFunctionFactory.Create(cardActions.SendNotificationAsync),

            // ── Compliance query tools ───────────────────────────────────
            AIFunctionFactory.Create(complianceTools.GetBvnVerificationAsync),
            AIFunctionFactory.Create(complianceTools.GetNinVerificationAsync),
            AIFunctionFactory.Create(complianceTools.GetFraudAssessmentAsync),
            AIFunctionFactory.Create(complianceTools.GetDigitalEnrollmentStatusAsync),
            AIFunctionFactory.Create(complianceTools.GetOnboardingMetricAsync),
            AIFunctionFactory.Create(complianceTools.GetOnboardingKpiSummaryAsync),

            // ── Compliance action tools ──────────────────────────────────
            AIFunctionFactory.Create(complianceActions.VerifyBvnAsync),
            AIFunctionFactory.Create(complianceActions.VerifyNinAsync),
            AIFunctionFactory.Create(complianceActions.RunFraudAssessmentAsync),
            AIFunctionFactory.Create(complianceActions.EnrollDigitalServicesAsync),
            AIFunctionFactory.Create(complianceActions.RecordFraudReviewOutcomeAsync),
            AIFunctionFactory.Create(complianceActions.RecordOnboardingMetricAsync),
        ];

        // Create the agent (stateless config; session carries the history)
        var agent = (ChatClientAgent)_anthropic.AsAIAgent(
            model:         _model,
            name:          "BankingAgent",
            instructions:  SystemPrompt,
            description:   "AI assistant for AIBanking operations",
            tools:         tools,
            loggerFactory: _loggerFactory);

        // Restore or create session
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        AgentSession session;
        if (_sessions.TryGetValue(conversationId, out var serialized))
            session = await agent.DeserializeSessionAsync(serialized, null, ct);
        else
            session = await agent.CreateSessionAsync(ct);

        // Run the agent with the user message
        var response = await agent.RunAsync(request.Message, session, options: null, ct);

        // Persist updated session
        _sessions[conversationId] = await agent.SerializeSessionAsync(session, null, ct);

        return new ChatResponse
        {
            Reply          = response.Text,
            ConversationId = conversationId
        };
    }

    public bool ClearConversation(string conversationId) =>
        _sessions.TryRemove(conversationId, out _);
}
