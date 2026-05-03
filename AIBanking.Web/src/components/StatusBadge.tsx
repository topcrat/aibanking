import type { AccountStatus, WorkflowStatus, ServiceProcessStatus, BvnVerificationStatus, FraudRiskLevel } from '../types';

type BadgeVariant = 'green' | 'yellow' | 'blue' | 'red' | 'gray' | 'orange' | 'purple';

const variantClasses: Record<BadgeVariant, string> = {
  green:  'bg-green-100 text-green-800 ring-green-200',
  yellow: 'bg-yellow-100 text-yellow-800 ring-yellow-200',
  blue:   'bg-blue-100 text-blue-800 ring-blue-200',
  red:    'bg-red-100 text-red-800 ring-red-200',
  gray:   'bg-gray-100 text-gray-700 ring-gray-200',
  orange: 'bg-orange-100 text-orange-800 ring-orange-200',
  purple: 'bg-purple-100 text-purple-800 ring-purple-200',
};

function badge(label: string, variant: BadgeVariant) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ring-1 ring-inset ${variantClasses[variant]}`}>
      {label}
    </span>
  );
}

export function AppStatusBadge({ status }: { status: AccountStatus }) {
  const map: Record<AccountStatus, [string, BadgeVariant]> = {
    Draft:            ['Draft', 'gray'],
    PendingDocuments: ['Pending Docs', 'yellow'],
    UnderReview:      ['Under Review', 'blue'],
    Approved:         ['Approved', 'purple'],
    Active:           ['Active', 'green'],
    Rejected:         ['Rejected', 'red'],
    Rework:           ['Rework', 'orange'],
  };
  const [label, variant] = map[status] ?? [status, 'gray'];
  return badge(label, variant);
}

export function WorkflowStatusBadge({ status }: { status: WorkflowStatus }) {
  const map: Record<WorkflowStatus, [string, BadgeVariant]> = {
    Pending:  ['Pending', 'yellow'],
    Approved: ['Approved', 'green'],
    Rework:   ['Rework', 'orange'],
    Declined: ['Declined', 'red'],
  };
  const [label, variant] = map[status] ?? [status, 'gray'];
  return badge(label, variant);
}

export function ProcessStatusBadge({ status }: { status: ServiceProcessStatus }) {
  const map: Record<ServiceProcessStatus, [string, BadgeVariant]> = {
    Pending:   ['Pending', 'gray'],
    Completed: ['Completed', 'green'],
    Failed:    ['Failed', 'red'],
  };
  const [label, variant] = map[status] ?? [status, 'gray'];
  return badge(label, variant);
}

export function BvnStatusBadge({ status }: { status: BvnVerificationStatus | string }) {
  const map: Record<string, [string, BadgeVariant]> = {
    NotStarted:  ['Not Started', 'gray'],
    Pending:     ['Pending', 'yellow'],
    Verified:    ['Verified', 'green'],
    Failed:      ['Failed', 'red'],
    Suspicious:  ['Suspicious', 'orange'],
  };
  const [label, variant] = map[status] ?? [status, 'gray'];
  return badge(label, variant);
}

export function FraudLevelBadge({ level }: { level: FraudRiskLevel | string }) {
  const map: Record<string, [string, BadgeVariant]> = {
    Low:      ['Low Risk', 'green'],
    Medium:   ['Medium Risk', 'yellow'],
    High:     ['High Risk', 'orange'],
    Critical: ['Critical Risk', 'red'],
  };
  const [label, variant] = map[level] ?? [level, 'gray'];
  return badge(label, variant);
}
