import { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  ArrowLeft, Upload, Cpu, CheckCircle2, XCircle, Clock,
  Bot, RefreshCw, User, CreditCard, ShieldCheck, AlertTriangle
} from 'lucide-react';
import { applicationsApi } from '../api/applications';
import type { AccountApplication, DocumentType } from '../types';
import { AppStatusBadge, ProcessStatusBadge } from '../components/StatusBadge';

type Tab = 'overview' | 'documents' | 'processes' | 'compliance';

const DOC_TYPES: { type: DocumentType; label: string }[] = [
  { type: 'AccountOpeningForm', label: 'Account Opening Form' },
  { type: 'IdentityCard',       label: 'Identity Card' },
];

export default function ApplicationDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [app,      setApp]      = useState<AccountApplication | null>(null);
  const [tab,      setTab]      = useState<Tab>('overview');
  const [loading,  setLoading]  = useState(true);
  const [error,    setError]    = useState('');
  const [success,  setSuccess]  = useState('');
  const [busy,     setBusy]     = useState('');

  async function load() {
    if (!id) return;
    setLoading(true);
    try {
      const data = await applicationsApi.getById(id);
      setApp(data);
    } catch {
      setError('Failed to load application.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, [id]); // eslint-disable-line react-hooks/exhaustive-deps

  function notify(msg: string) {
    setSuccess(msg);
    setTimeout(() => setSuccess(''), 4000);
  }

  async function handleUpload(docType: DocumentType, file: File) {
    if (!id) return;
    setError('');
    setBusy(`upload-${docType}`);
    try {
      await applicationsApi.uploadDocument(id, file, docType);
      await load();
      notify('Document uploaded.');
    } catch {
      setError('Upload failed. Please try again.');
    } finally {
      setBusy('');
    }
  }

  async function handleExtract() {
    if (!id) return;
    setError('');
    setBusy('extract');
    try {
      const updated = await applicationsApi.extract(id);
      setApp(updated);
      notify('Extraction complete.');
    } catch {
      setError('Extraction failed. Ensure both documents are uploaded.');
    } finally {
      setBusy('');
    }
  }

  async function handleCreateCustomer() {
    if (!id) return;
    setError('');
    setBusy('customer');
    try {
      await applicationsApi.createCustomer(id);
      await load();
      notify('Customer record created.');
    } catch {
      setError('Failed to create customer. Check that extraction has been completed.');
    } finally {
      setBusy('');
    }
  }

  async function handleCreateAccount() {
    if (!id) return;
    setError('');
    setBusy('account');
    try {
      await applicationsApi.createAccount(id);
      await load();
      notify('Bank account created.');
    } catch {
      setError('Failed to create account. CreateCustomer must be completed first.');
    } finally {
      setBusy('');
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="animate-spin w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full" />
      </div>
    );
  }

  if (error || !app) {
    return (
      <div className="p-8">
        <p className="text-red-600">{error || 'Application not found.'}</p>
        <button onClick={() => navigate('/applications')} className="mt-4 text-blue-600 text-sm">
          ← Back to applications
        </button>
      </div>
    );
  }

  const customerProcess = app.processes.find(p => p.name === 'CreateCustomer');
  const accountProcess  = app.processes.find(p => p.name === 'CreateAccount');

  return (
    <div className="p-8">
      {/* Breadcrumb */}
      <button
        onClick={() => navigate('/applications')}
        className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-gray-700 mb-6"
      >
        <ArrowLeft size={16} /> Applications
      </button>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-xl font-bold text-gray-900 font-mono">{app.id.slice(0, 13)}…</h1>
            <AppStatusBadge status={app.status} />
          </div>
          <p className="text-sm text-gray-400 mt-1">
            Created {new Date(app.createdAt).toLocaleString()} ·
            Updated {new Date(app.updatedAt).toLocaleString()}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => navigate(`/agent?appId=${app.id}`)}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
          >
            <Bot size={15} />
            Process with AI
          </button>
          <button
            onClick={load}
            className="p-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-gray-500"
            title="Refresh"
          >
            <RefreshCw size={16} />
          </button>
        </div>
      </div>

      {/* Inline error / success banners */}
      {error && (
        <div className="mb-4 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm flex items-center justify-between">
          <span>{error}</span>
          <button onClick={() => setError('')} className="ml-4 text-red-400 hover:text-red-600 font-bold">✕</button>
        </div>
      )}
      {success && (
        <div className="mb-4 p-3 rounded-lg bg-green-50 border border-green-200 text-green-700 text-sm flex items-center justify-between">
          <span>{success}</span>
          <button onClick={() => setSuccess('')} className="ml-4 text-green-400 hover:text-green-600 font-bold">✕</button>
        </div>
      )}

      {/* Rework notes */}
      {app.reworkNotes && (
        <div className="mb-5 p-4 bg-orange-50 border border-orange-200 rounded-lg">
          <p className="text-sm font-medium text-orange-800 mb-1">Rework Required</p>
          <pre className="text-xs text-orange-700 whitespace-pre-wrap font-mono">{app.reworkNotes}</pre>
        </div>
      )}

      {/* Tabs */}
      <div className="flex gap-1 border-b border-gray-200 mb-6">
        {([
          { key: 'overview',    label: 'Overview' },
          { key: 'documents',   label: 'Documents' },
          { key: 'processes',   label: 'Processes' },
          { key: 'compliance',  label: 'KYC & Compliance' },
        ] as { key: Tab; label: string }[]).map(({ key, label }) => (
          <button
            key={key}
            onClick={() => setTab(key)}
            className={`px-4 py-2.5 text-sm font-medium transition-colors border-b-2 -mb-px ${
              tab === key
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {/* ── OVERVIEW ── */}
      {tab === 'overview' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Extracted Info */}
          <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
            <div className="flex items-center gap-2 mb-4">
              <User size={18} className="text-gray-400" />
              <h3 className="font-semibold text-gray-900">Extracted Information</h3>
            </div>
            {app.extractedInfo ? (
              <dl className="space-y-3">
                {([
                  ['Full Name',       app.extractedInfo.fullName],
                  ['Date of Birth',   app.extractedInfo.dateOfBirth],
                  ['Gender',         app.extractedInfo.gender],
                  ['Phone',          app.extractedInfo.phoneNumber],
                  ['Address',        app.extractedInfo.residenceAddress],
                  ['NIN (Extracted)', app.extractedInfo.nationalIdNumber],
                ] as [string, string | undefined][]).map(([label, val]) => (
                  <div key={label} className="flex justify-between text-sm">
                    <dt className="text-gray-500">{label}</dt>
                    <dd className="font-medium text-gray-900 text-right max-w-[200px] truncate">
                      {val ?? <span className="text-gray-400 italic font-normal">—</span>}
                    </dd>
                  </div>
                ))}
              </dl>
            ) : (
              <p className="text-sm text-gray-400 italic">
                No data extracted yet. Upload documents and run extraction.
              </p>
            )}
          </div>

          {/* Process summary */}
          <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
            <div className="flex items-center gap-2 mb-4">
              <CreditCard size={18} className="text-gray-400" />
              <h3 className="font-semibold text-gray-900">Service Processes</h3>
            </div>
            <div className="space-y-3">
              {app.processes.map(p => (
                <div key={p.name} className="flex items-center justify-between text-sm">
                  <span className="text-gray-700">{p.name}</span>
                  <ProcessStatusBadge status={p.status} />
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* ── DOCUMENTS ── */}
      {tab === 'documents' && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
            {DOC_TYPES.map(({ type, label }) => {
              const uploaded = app.documents.find(d => d.type === type);
              return (
                <DocumentUploadCard
                  key={type}
                  label={label}
                  uploaded={uploaded ? { name: uploaded.fileName, date: uploaded.uploadedAt } : null}
                  busy={busy === `upload-${type}`}
                  onUpload={(file) => handleUpload(type, file)}
                />
              );
            })}
          </div>

          <div className="flex items-center gap-4">
            <button
              onClick={handleExtract}
              disabled={busy === 'extract' || app.documents.length < 2}
              className="flex items-center gap-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors"
            >
              <Cpu size={15} />
              {busy === 'extract' ? 'Extracting…' : 'Run AI Extraction'}
            </button>
            {app.documents.length < 2 && (
              <p className="text-sm text-orange-600">Upload both documents before running extraction.</p>
            )}
          </div>

          {app.extractedInfo && (
            <div className="bg-green-50 border border-green-200 rounded-lg p-4">
              <div className="flex items-center gap-2 text-green-700 text-sm font-medium mb-1">
                <CheckCircle2 size={15} /> Extraction complete
              </div>
              <p className="text-sm text-green-600">
                Full Name: <strong>{app.extractedInfo.fullName ?? 'N/A'}</strong>
              </p>
            </div>
          )}
        </div>
      )}

      {/* ── PROCESSES ── */}
      {tab === 'processes' && (
        <div className="space-y-4">
          <ProcessCard
            title="Process 1 — Create Customer"
            description="Creates a Customer record from the AI-extracted personal information."
            status={customerProcess?.status ?? 'Pending'}
            resultId={customerProcess?.resultId}
            resultLabel="Customer ID"
            completedAt={customerProcess?.completedAt}
            error={customerProcess?.error}
            canRun={app.status !== 'Draft' && app.extractedInfo !== null && customerProcess?.status !== 'Completed'}
            busy={busy === 'customer'}
            onRun={handleCreateCustomer}
          />
          <ProcessCard
            title="Process 2 — Create Account"
            description="Opens a bank account linked to the customer. Also auto-generates a debit card request and sets up notification preferences."
            status={accountProcess?.status ?? 'Pending'}
            resultId={accountProcess?.resultId}
            resultLabel="Account ID"
            completedAt={accountProcess?.completedAt}
            error={accountProcess?.error}
            canRun={customerProcess?.status === 'Completed' && accountProcess?.status !== 'Completed'}
            busy={busy === 'account'}
            onRun={handleCreateAccount}
          />
        </div>
      )}

      {/* ── KYC & COMPLIANCE ── */}
      {tab === 'compliance' && (
        <div className="space-y-5">
          {/* Identity Numbers */}
          <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
            <div className="flex items-center gap-2 mb-4">
              <ShieldCheck size={18} className="text-gray-400" />
              <h3 className="font-semibold text-gray-900">Identity Numbers</h3>
            </div>
            <dl className="space-y-3">
              {([
                ['BVN (Bank Verification Number)', app.bvnNumber],
                ['NIN (National ID Number)', app.ninNumber ?? app.extractedInfo?.nationalIdNumber],
              ] as [string, string | undefined][]).map(([label, val]) => (
                <div key={label} className="flex justify-between text-sm">
                  <dt className="text-gray-500">{label}</dt>
                  <dd className="font-mono font-medium text-gray-900">
                    {val ?? <span className="text-gray-400 italic font-sans font-normal">Not provided</span>}
                  </dd>
                </div>
              ))}
            </dl>
          </div>

          {/* KYC Tier */}
          <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
            <div className="flex items-center gap-2 mb-4">
              <CreditCard size={18} className="text-gray-400" />
              <h3 className="font-semibold text-gray-900">CBN KYC Tier</h3>
            </div>
            <p className="text-sm text-gray-500 mb-4">
              Tier is auto-assigned when the customer record is created based on available verified data.
            </p>
            <div className="grid grid-cols-3 gap-3">
              {([1, 2, 3] as const).map(tier => {
                const info = {
                  1: { label: 'Tier 1', desc: 'BVN/NIN + basic bio-data', limits: '₦50k / ₦300k / ₦300k max', color: 'blue' },
                  2: { label: 'Tier 2', desc: 'Govt ID + verified address', limits: '₦200k / ₦1M / ₦5M max', color: 'indigo' },
                  3: { label: 'Tier 3', desc: 'Full KYC', limits: '₦10M / ₦50M / unlimited', color: 'purple' },
                }[tier];
                return (
                  <div key={tier} className={`rounded-lg border-2 p-4 ${
                    info.color === 'blue'   ? 'border-blue-200 bg-blue-50' :
                    info.color === 'indigo' ? 'border-indigo-200 bg-indigo-50' :
                                             'border-purple-200 bg-purple-50'
                  }`}>
                    <p className={`font-semibold text-sm ${
                      info.color === 'blue'   ? 'text-blue-800' :
                      info.color === 'indigo' ? 'text-indigo-800' : 'text-purple-800'
                    }`}>{info.label}</p>
                    <p className="text-xs text-gray-600 mt-1">{info.desc}</p>
                    <p className="text-xs text-gray-500 mt-1 font-mono">{info.limits}</p>
                    <p className="text-xs text-gray-400 mt-0.5">single / daily / balance</p>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Consent */}
          <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
            <div className="flex items-center gap-2 mb-3">
              {app.consentGiven
                ? <CheckCircle2 size={18} className="text-green-500" />
                : <AlertTriangle size={18} className="text-orange-400" />}
              <h3 className="font-semibold text-gray-900">NDPA Data Consent</h3>
            </div>
            {app.consentGiven ? (
              <p className="text-sm text-green-700 bg-green-50 rounded-lg px-3 py-2">
                Applicant has given explicit data-usage consent as required by the Nigeria Data Protection Act.
              </p>
            ) : (
              <p className="text-sm text-orange-700 bg-orange-50 rounded-lg px-3 py-2">
                Consent not yet captured. This is flagged as a compliance signal in fraud assessment. Obtain consent before account activation.
              </p>
            )}
          </div>

          {/* Compliance checklist */}
          <div className="bg-white rounded-xl border border-gray-200 p-6 shadow-sm">
            <div className="flex items-center gap-2 mb-4">
              <ShieldCheck size={18} className="text-gray-400" />
              <h3 className="font-semibold text-gray-900">Compliance Checklist</h3>
            </div>
            <div className="space-y-2">
              {[
                { label: 'Account Opening Form uploaded', done: app.documents.some(d => d.type === 'AccountOpeningForm') },
                { label: 'Identity Card uploaded',       done: app.documents.some(d => d.type === 'IdentityCard') },
                { label: 'AI extraction completed',     done: !!app.extractedInfo },
                { label: 'Full Name extracted',         done: !!app.extractedInfo?.fullName },
                { label: 'NIN provided',                done: !!(app.ninNumber || app.extractedInfo?.nationalIdNumber) },
                { label: 'BVN provided',                done: !!app.bvnNumber },
                { label: 'NDPA consent captured',       done: app.consentGiven },
              ].map(({ label, done }) => (
                <div key={label} className="flex items-center gap-2.5 text-sm">
                  {done
                    ? <CheckCircle2 size={15} className="text-green-500 shrink-0" />
                    : <XCircle size={15} className="text-gray-300 shrink-0" />
                  }
                  <span className={done ? 'text-gray-800' : 'text-gray-400'}>{label}</span>
                </div>
              ))}
            </div>
            <p className="mt-4 text-xs text-gray-400">
              BVN/NIN verification status and fraud assessment are managed via the AI Agent.
            </p>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function DocumentUploadCard({
  label, uploaded, busy, onUpload
}: {
  label: string;
  uploaded: { name: string; date: string } | null;
  busy: boolean;
  onUpload: (file: File) => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);

  return (
    <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
      <div className="flex items-start justify-between mb-3">
        <div>
          <p className="font-medium text-gray-900 text-sm">{label}</p>
          {uploaded ? (
            <p className="text-xs text-gray-400 mt-0.5">
              <span className="font-mono">{uploaded.name}</span> · {new Date(uploaded.date).toLocaleDateString()}
            </p>
          ) : (
            <p className="text-xs text-gray-400 mt-0.5 italic">Not uploaded</p>
          )}
        </div>
        {uploaded
          ? <CheckCircle2 size={18} className="text-green-500 shrink-0" />
          : <XCircle size={18} className="text-gray-300 shrink-0" />
        }
      </div>
      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp,application/pdf"
        className="hidden"
        onChange={e => { const f = e.target.files?.[0]; if (f) onUpload(f); e.target.value = ''; }}
      />
      <button
        onClick={() => inputRef.current?.click()}
        disabled={busy}
        className="flex items-center gap-1.5 text-sm text-blue-600 hover:text-blue-700 disabled:opacity-50 font-medium"
      >
        <Upload size={14} />
        {busy ? 'Uploading…' : uploaded ? 'Replace' : 'Upload'}
      </button>
    </div>
  );
}

function ProcessCard({
  title, description, status, resultId, resultLabel,
  completedAt, error, canRun, busy, onRun
}: {
  title: string;
  description: string;
  status: string;
  resultId?: string;
  resultLabel: string;
  completedAt?: string;
  error?: string;
  canRun: boolean;
  busy: boolean;
  onRun: () => void;
}) {
  const icon =
    status === 'Completed' ? <CheckCircle2 size={20} className="text-green-500" /> :
    status === 'Failed'    ? <XCircle size={20} className="text-red-500" /> :
                             <Clock size={20} className="text-gray-400" />;

  return (
    <div className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
      <div className="flex items-start gap-4">
        <div className="mt-0.5 shrink-0">{icon}</div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between gap-4">
            <p className="font-semibold text-gray-900 text-sm">{title}</p>
            <ProcessStatusBadge status={status as 'Pending' | 'Completed' | 'Failed'} />
          </div>
          <p className="text-sm text-gray-500 mt-1">{description}</p>
          {resultId && (
            <p className="text-xs mt-2 text-gray-500">
              {resultLabel}: <span className="font-mono text-gray-700">{resultId}</span>
            </p>
          )}
          {completedAt && (
            <p className="text-xs text-gray-400 mt-1">
              Completed {new Date(completedAt).toLocaleString()}
            </p>
          )}
          {error && (
            <p className="text-xs text-red-600 mt-2 bg-red-50 px-2 py-1 rounded">{error}</p>
          )}
          {canRun && (
            <button
              onClick={onRun}
              disabled={busy}
              className="mt-3 flex items-center gap-1.5 px-3 py-1.5 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-xs font-medium rounded-lg transition-colors"
            >
              {busy ? 'Running…' : 'Run Process'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
