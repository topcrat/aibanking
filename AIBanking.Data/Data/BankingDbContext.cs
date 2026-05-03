using AIBanking.Models;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Data;

public class BankingDbContext : DbContext
{
    public BankingDbContext(DbContextOptions<BankingDbContext> options) : base(options) { }

    public DbSet<User>                 Users                { get; set; }
    public DbSet<AccountApplication>  AccountApplications  { get; set; }
    public DbSet<ApplicationDocument> ApplicationDocuments { get; set; }
    public DbSet<ApplicationProcess>  ApplicationProcesses { get; set; }
    public DbSet<Customer>            Customers            { get; set; }
    public DbSet<BankAccount>         BankAccounts         { get; set; }
    public DbSet<WorkflowDefinition>      WorkflowDefinitions      { get; set; }
    public DbSet<WorkflowStageDefinition> WorkflowStageDefinitions { get; set; }
    public DbSet<WorkflowItem>            WorkflowItems            { get; set; }
    public DbSet<WorkflowDocument>        WorkflowDocuments        { get; set; }
    public DbSet<WorkflowApproval>        WorkflowApprovals        { get; set; }
    public DbSet<FormDefinition>          FormDefinitions          { get; set; }
    public DbSet<FormFieldDefinition>     FormFieldDefinitions     { get; set; }
    public DbSet<FormSubmission>          FormSubmissions          { get; set; }
    public DbSet<CardRequest>            CardRequests            { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<NotificationLog>        NotificationLogs        { get; set; }
    public DbSet<BvnVerification>        BvnVerifications        { get; set; }
    public DbSet<NinVerification>        NinVerifications        { get; set; }
    public DbSet<DigitalEnrollment>      DigitalEnrollments      { get; set; }
    public DbSet<FraudAssessment>        FraudAssessments        { get; set; }
    public DbSet<OnboardingMetric>       OnboardingMetrics       { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankingDbContext).Assembly);
    }
}
