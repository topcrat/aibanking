// ── Enums ────────────────────────────────────────────────────────────────────

export type AccountStatus =
  | 'Draft'
  | 'PendingDocuments'
  | 'UnderReview'
  | 'Approved'
  | 'Active'
  | 'Rejected'
  | 'Rework';

export type DocumentType = 'AccountOpeningForm' | 'IdentityCard';

export type ServiceProcess = 'CreateCustomer' | 'CreateAccount';
export type ServiceProcessStatus = 'Pending' | 'Completed' | 'Failed';

export type WorkflowStatus = 'Pending' | 'Approved' | 'Rework' | 'Declined';

export type BvnVerificationStatus =
  | 'NotStarted'
  | 'Pending'
  | 'Verified'
  | 'Failed'
  | 'Suspicious';

export type NinVerificationStatus = BvnVerificationStatus;

export type KycTier = 1 | 2 | 3;

export type FraudRiskLevel = 'Low' | 'Medium' | 'High' | 'Critical';

export type DigitalServiceType = 'MobileBanking' | 'InternetBanking';
export type DigitalEnrollmentStatus = 'Pending' | 'Active' | 'Suspended' | 'Cancelled';

// ── Models ───────────────────────────────────────────────────────────────────

export interface ExtractedPersonInfo {
  fullName?: string;
  dateOfBirth?: string;
  gender?: string;
  phoneNumber?: string;
  residenceAddress?: string;
  nationalIdNumber?: string;
}

export interface DocumentSummary {
  id: string;
  type: DocumentType;
  fileName: string;
  contentType: string;
  uploadedAt: string;
}

export interface ProcessSummary {
  name: ServiceProcess;
  status: ServiceProcessStatus;
  resultId?: string;
  completedAt?: string;
  error?: string;
}

export interface AccountApplication {
  id: string;
  status: AccountStatus;
  extractedInfo?: ExtractedPersonInfo;
  documents: DocumentSummary[];
  processes: ProcessSummary[];
  bvnNumber?: string;
  ninNumber?: string;
  consentGiven: boolean;
  reworkNotes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface Customer {
  id: string;
  applicationId: string;
  fullName: string;
  dateOfBirth?: string;
  gender?: string;
  phoneNumber?: string;
  residenceAddress?: string;
  nationalIdNumber?: string;
  bvnNumber?: string;
  kycTier: KycTier;
  createdAt: string;
}

export interface BankAccount {
  id: string;
  accountNumber: string;
  customerId: string;
  applicationId: string;
  accountType: string;
  kycTier: KycTier;
  singleTransactionLimit: number;
  dailyTransactionLimit: number;
  maximumBalance: number;
  createdAt: string;
}


export interface WorkflowStageDefinition {
  id: string;
  definitionId: string;
  stageOrder: number;
  stageName: string;
  requiredRole: string;
}

export interface WorkflowDefinition {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  stages: WorkflowStageDefinition[];
}

export interface WorkflowApproval {
  id: string;
  workflowItemId: string;
  stageOrder: number;
  stageName: string;
  action: 'Approved' | 'Rework' | 'Declined';
  actedBy: string;
  comments?: string;
  actedAt: string;
}

export interface WorkflowItem {
  id: string;
  definitionId: string;
  title: string;
  description?: string;
  submittedBy: string;
  status: WorkflowStatus;
  currentStageOrder: number;
  reviewedBy?: string;
  comments?: string;
  createdAt: string;
  updatedAt: string;
  definition?: WorkflowDefinition;
  approvals?: WorkflowApproval[];
  formSubmission?: FormSubmission;
}

export interface WorkflowDocument {
  id: string;
  workflowId: string;
  fileName: string;
  contentType: string;
  uploadedBy: string;
  uploadedAt: string;
}

// ── Forms ─────────────────────────────────────────────────────────────────────

export type FormFieldType = 'Text' | 'TextArea' | 'Number' | 'Date' | 'Select' | 'File';

export interface FormFieldDefinition {
  id: string;
  formDefinitionId: string;
  fieldOrder: number;
  fieldKey: string;
  label: string;
  fieldType: FormFieldType;
  isRequired: boolean;
  placeholder?: string;
  optionsJson?: string;
}

export interface FormDefinition {
  id: string;
  name: string;
  description: string;
  workflowDefinitionId: string;
  isActive: boolean;
  createdAt: string;
  fields: FormFieldDefinition[];
  workflowDefinition?: WorkflowDefinition;
}

export interface FormSubmission {
  id: string;
  formDefinitionId: string;
  workflowItemId: string;
  submittedBy: string;
  submittedAt: string;
  valuesJson: string;
  formDefinition?: FormDefinition;
  workflowItem?: WorkflowItem;
}

// ── Chat ─────────────────────────────────────────────────────────────────────

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

export interface ChatResponse {
  reply: string;
  conversationId: string;
}
