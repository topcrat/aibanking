namespace AIBanking.Models;

public class FormDefinition
{
    public Guid   Id                 { get; set; }
    public string Name               { get; set; } = string.Empty;
    public string Description        { get; set; } = string.Empty;
    public Guid   WorkflowDefinitionId { get; set; }
    public bool   IsActive           { get; set; } = true;
    public DateTime CreatedAt        { get; set; }

    public WorkflowDefinition            WorkflowDefinition { get; set; } = null!;
    public ICollection<FormFieldDefinition> Fields          { get; set; } = [];
}

public class FormFieldDefinition
{
    public Guid        Id               { get; set; }
    public Guid        FormDefinitionId { get; set; }
    public int         FieldOrder       { get; set; }
    public string      FieldKey         { get; set; } = string.Empty;
    public string      Label            { get; set; } = string.Empty;
    public FormFieldType FieldType      { get; set; }
    public bool        IsRequired       { get; set; }
    public string?     Placeholder      { get; set; }
    public string?     OptionsJson      { get; set; } // JSON array of strings for Select type

    public FormDefinition FormDefinition { get; set; } = null!;
}

public enum FormFieldType
{
    Text,
    TextArea,
    Number,
    Date,
    Select,
    File,
}
