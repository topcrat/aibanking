import { useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Upload, X, CheckCircle, ChevronRight, ChevronLeft, Loader2,
} from 'lucide-react';
import { formsApi } from '../api/forms';
import { workflowsApi } from '../api/workflows';
import { getAxiosErrorMessage } from '../utils/errorHandling';

// ── Seeded form/workflow GUIDs ────────────────────────────────────────────────

const LOAN_FORM_ID = '33333333-0000-0000-0000-000000000002';

// ── Types ─────────────────────────────────────────────────────────────────────

interface LoanInfo {
  customerName:   string;
  accountNumber:  string;
  bvnNumber:      string;
  loanType:       string;
  loanAmount:     string;
  loanTenor:      string;
  loanPurpose:    string;
  monthlyIncome:  string;
  employerName:   string;
  collateralType: string;
  collateralValue: string;
}

const LOAN_TYPES      = ['Personal', 'Business', 'Mortgage', 'Auto', 'Education'];
const COLLATERAL_TYPES = ['None', 'Property', 'Vehicle', 'Equipment', 'Other'];
const STEPS           = ['Loan Details', 'Financial Info', 'Documents', 'Review & Submit'];
const ACCEPTED        = 'image/jpeg,image/png,image/webp,application/pdf';

// ── Component ─────────────────────────────────────────────────────────────────

export default function LoanBookingForm() {
  const navigate = useNavigate();
  const [step, setStep] = useState(0);

  const [info, setInfo] = useState<LoanInfo>({
    customerName:    '',
    accountNumber:   '',
    bvnNumber:       '',
    loanType:        '',
    loanAmount:      '',
    loanTenor:       '',
    loanPurpose:     '',
    monthlyIncome:   '',
    employerName:    '',
    collateralType:  'None',
    collateralValue: '',
  });

  const [files, setFiles] = useState<File[]>([]);
  const [errors, setErrors] = useState<Partial<Record<string, string>>>({});
  const [submitting, setSubmitting] = useState(false);
  const [progress, setProgress] = useState('');
  const fileInputRef = useRef<HTMLInputElement>(null);

  // ── Validation ──────────────────────────────────────────────────────────────

  function validateStep0(): boolean {
    const e: typeof errors = {};
    if (!info.customerName.trim())  e.customerName  = 'Customer name is required.';
    if (!info.accountNumber.trim()) e.accountNumber = 'Account number is required.';
    if (!/^\d{11}$/.test(info.bvnNumber)) e.bvnNumber = 'BVN must be exactly 11 digits.';
    if (!info.loanType)             e.loanType      = 'Loan type is required.';
    if (!info.loanAmount || isNaN(+info.loanAmount) || +info.loanAmount <= 0)
      e.loanAmount = 'Enter a valid loan amount.';
    if (!info.loanTenor || isNaN(+info.loanTenor) || +info.loanTenor < 1)
      e.loanTenor = 'Enter a valid tenor (minimum 1 month).';
    if (!info.loanPurpose.trim())   e.loanPurpose   = 'Loan purpose is required.';
    setErrors(e);
    return Object.keys(e).length === 0;
  }

  function validateStep1(): boolean {
    const e: typeof errors = {};
    if (!info.monthlyIncome || isNaN(+info.monthlyIncome) || +info.monthlyIncome <= 0)
      e.monthlyIncome = 'Enter a valid monthly income.';
    if (!info.employerName.trim()) e.employerName = 'Employer / business name is required.';
    if (info.collateralType !== 'None' && info.collateralValue) {
      if (isNaN(+info.collateralValue) || +info.collateralValue < 0)
        e.collateralValue = 'Enter a valid collateral value.';
    }
    setErrors(e);
    return Object.keys(e).length === 0;
  }

  function validateStep2(): boolean {
    const e: typeof errors = {};
    if (files.length === 0) e.files = 'At least one supporting document is required.';
    setErrors(e);
    return Object.keys(e).length === 0;
  }

  // ── Navigation ──────────────────────────────────────────────────────────────

  function next() {
    if (step === 0 && !validateStep0()) return;
    if (step === 1 && !validateStep1()) return;
    if (step === 2 && !validateStep2()) return;
    setStep(s => s + 1);
  }

  function back() {
    setErrors({});
    setStep(s => s - 1);
  }

  // ── File handling ───────────────────────────────────────────────────────────

  function addFiles(incoming: FileList | null) {
    if (!incoming) return;
    setFiles(prev => {
      const existing = new Set(prev.map(f => f.name));
      const added = Array.from(incoming).filter(f => !existing.has(f.name));
      return [...prev, ...added];
    });
    setErrors(e => ({ ...e, files: undefined }));
  }

  function removeFile(index: number) {
    setFiles(prev => prev.filter((_, i) => i !== index));
  }

  // ── Submit ──────────────────────────────────────────────────────────────────

  async function handleSubmit() {
    setSubmitting(true);
    setErrors({});
    try {
      setProgress('Submitting loan application…');
      const values: Record<string, string> = {
        customerName:    info.customerName,
        accountNumber:   info.accountNumber,
        bvnNumber:       info.bvnNumber,
        loanType:        info.loanType,
        loanAmount:      info.loanAmount,
        loanTenor:       info.loanTenor,
        loanPurpose:     info.loanPurpose,
        monthlyIncome:   info.monthlyIncome,
        employerName:    info.employerName,
        collateralType:  info.collateralType,
        ...(info.collateralValue ? { collateralValue: info.collateralValue } : {}),
        supportingDoc:   files.map(f => f.name).join(', '),
      };

      const { workflowItem } = await formsApi.submit(LOAN_FORM_ID, values);
      const workflowId = (workflowItem as { id: string }).id;

      for (let i = 0; i < files.length; i++) {
        setProgress(`Uploading document ${i + 1} of ${files.length}…`);
        await workflowsApi.uploadDocument(workflowId, files[i]);
      }

      navigate(`/workflows/${workflowId}`);
    } catch (err: unknown) {
      setErrors({ submit: getAxiosErrorMessage(err, 'Submission failed. Please try again.') });
      setSubmitting(false);
      setProgress('');
    }
  }

  // ── Field helpers ───────────────────────────────────────────────────────────

  const set = (key: keyof LoanInfo) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) =>
    setInfo(i => ({ ...i, [key]: e.target.value }));

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div className="min-h-full p-8 flex flex-col items-center">
      <div className="w-full max-w-2xl">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Loan Booking</h1>
          <p className="text-gray-500 mt-1">
            Complete this form to submit a loan application for approval.
          </p>
        </div>

        {/* Stepper */}
        <div className="flex items-center gap-0 mb-8">
          {STEPS.map((label, i) => (
            <div key={i} className="flex items-center flex-1 last:flex-none">
              <div className={`flex items-center gap-2 ${i <= step ? 'text-blue-600' : 'text-gray-400'}`}>
                <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-semibold border-2 transition-colors
                  ${i < step  ? 'bg-blue-600 border-blue-600 text-white'
                  : i === step ? 'bg-white border-blue-600 text-blue-600'
                  : 'bg-white border-gray-300 text-gray-400'}`}
                >
                  {i < step ? <CheckCircle size={14} /> : i + 1}
                </div>
                <span className="text-sm font-medium hidden sm:block">{label}</span>
              </div>
              {i < STEPS.length - 1 && (
                <div className={`flex-1 h-0.5 mx-3 transition-colors ${i < step ? 'bg-blue-600' : 'bg-gray-200'}`} />
              )}
            </div>
          ))}
        </div>

        {/* Card */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">

          {/* ── Step 0: Loan Details ── */}
          {step === 0 && (
            <div className="space-y-5">
              <h2 className="text-lg font-semibold text-gray-800">Loan Details</h2>

              <Field label="Customer Full Name *" error={errors.customerName}>
                <input type="text" value={info.customerName} onChange={set('customerName')}
                  placeholder="As it appears on ID" className={cls(errors.customerName)} />
              </Field>

              <div className="grid grid-cols-2 gap-4">
                <Field label="Account Number *" error={errors.accountNumber}>
                  <input type="text" value={info.accountNumber} onChange={set('accountNumber')}
                    placeholder="BNK…" className={cls(errors.accountNumber)} />
                </Field>
                <Field label="BVN (11 digits) *" error={errors.bvnNumber}>
                  <input type="text" inputMode="numeric" maxLength={11}
                    value={info.bvnNumber}
                    onChange={e => setInfo(i => ({ ...i, bvnNumber: e.target.value.replace(/\D/g, '') }))}
                    placeholder="22200000000" className={cls(errors.bvnNumber)} />
                </Field>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <Field label="Loan Type *" error={errors.loanType}>
                  <select value={info.loanType} onChange={set('loanType')} className={cls(errors.loanType)}>
                    <option value="">Select…</option>
                    {LOAN_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                  </select>
                </Field>
                <Field label="Loan Tenor (months) *" error={errors.loanTenor}>
                  <input type="number" min={1} value={info.loanTenor} onChange={set('loanTenor')}
                    placeholder="e.g. 12" className={cls(errors.loanTenor)} />
                </Field>
              </div>

              <Field label="Loan Amount (₦) *" error={errors.loanAmount}>
                <input type="number" min={1} value={info.loanAmount} onChange={set('loanAmount')}
                  placeholder="e.g. 500000" className={cls(errors.loanAmount)} />
              </Field>

              <Field label="Purpose of Loan *" error={errors.loanPurpose}>
                <textarea rows={3} value={info.loanPurpose} onChange={set('loanPurpose')}
                  placeholder="Describe the intended use of funds"
                  className={cls(errors.loanPurpose)} />
              </Field>
            </div>
          )}

          {/* ── Step 1: Financial Info ── */}
          {step === 1 && (
            <div className="space-y-5">
              <h2 className="text-lg font-semibold text-gray-800">Financial Information</h2>

              <div className="grid grid-cols-2 gap-4">
                <Field label="Monthly Income (₦) *" error={errors.monthlyIncome}>
                  <input type="number" min={0} value={info.monthlyIncome} onChange={set('monthlyIncome')}
                    placeholder="e.g. 150000" className={cls(errors.monthlyIncome)} />
                </Field>
                <Field label="Employer / Business *" error={errors.employerName}>
                  <input type="text" value={info.employerName} onChange={set('employerName')}
                    placeholder="Employer or business name" className={cls(errors.employerName)} />
                </Field>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <Field label="Collateral Type" error={errors.collateralType}>
                  <select value={info.collateralType} onChange={set('collateralType')} className={cls(errors.collateralType)}>
                    {COLLATERAL_TYPES.map(t => <option key={t} value={t}>{t}</option>)}
                  </select>
                </Field>
                {info.collateralType !== 'None' && (
                  <Field label="Collateral Value (₦)" error={errors.collateralValue}>
                    <input type="number" min={0} value={info.collateralValue} onChange={set('collateralValue')}
                      placeholder="Estimated market value" className={cls(errors.collateralValue)} />
                  </Field>
                )}
              </div>

              {/* Indicative repayment */}
              {info.loanAmount && info.loanTenor && (
                <div className="rounded-lg bg-blue-50 border border-blue-200 p-4">
                  <p className="text-sm font-medium text-blue-800 mb-1">Indicative Repayment</p>
                  <div className="grid grid-cols-3 gap-4 text-sm text-blue-700">
                    <div>
                      <span className="block text-xs text-blue-500">Loan Amount</span>
                      ₦{Number(info.loanAmount).toLocaleString()}
                    </div>
                    <div>
                      <span className="block text-xs text-blue-500">Tenor</span>
                      {info.loanTenor} months
                    </div>
                    <div>
                      <span className="block text-xs text-blue-500">Monthly (est.)</span>
                      ₦{Math.ceil(+info.loanAmount / +info.loanTenor).toLocaleString()}
                    </div>
                  </div>
                  <p className="text-xs text-blue-400 mt-2">Interest rate and fees are subject to credit assessment.</p>
                </div>
              )}
            </div>
          )}

          {/* ── Step 2: Documents ── */}
          {step === 2 && (
            <div className="space-y-5">
              <h2 className="text-lg font-semibold text-gray-800">Supporting Documents</h2>
              <p className="text-sm text-gray-500">
                Upload relevant documents: bank statements, pay slips, collateral proof, ID. Accepted formats: JPEG, PNG, WebP, PDF.
              </p>

              <div
                onDragOver={e => e.preventDefault()}
                onDrop={e => { e.preventDefault(); addFiles(e.dataTransfer.files); }}
                onClick={() => fileInputRef.current?.click()}
                className={`border-2 border-dashed rounded-lg px-6 py-10 text-center cursor-pointer transition-colors hover:border-blue-400 hover:bg-blue-50 ${
                  errors.files ? 'border-red-400 bg-red-50' : 'border-gray-300 bg-gray-50'
                }`}
              >
                <Upload size={28} className={`mx-auto mb-2 ${errors.files ? 'text-red-400' : 'text-gray-400'}`} />
                <p className="text-sm text-gray-600">
                  <span className="font-medium text-blue-600">Click to browse</span> or drag & drop
                </p>
                <p className="text-xs text-gray-400 mt-1">Multiple files supported</p>
              </div>

              <input
                ref={fileInputRef}
                type="file"
                accept={ACCEPTED}
                multiple
                className="hidden"
                onChange={e => { addFiles(e.target.files); e.target.value = ''; }}
              />

              {errors.files && <p className="text-xs text-red-600">{errors.files}</p>}

              {files.length > 0 && (
                <ul className="space-y-2">
                  {files.map((f, i) => (
                    <li key={i} className="flex items-center gap-3 px-4 py-2.5 border border-blue-200 bg-blue-50 rounded-lg">
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-blue-800 truncate">{f.name}</p>
                        <p className="text-xs text-blue-500">{(f.size / 1024).toFixed(0)} KB</p>
                      </div>
                      <button onClick={() => removeFile(i)} className="text-blue-400 hover:text-red-500 transition-colors">
                        <X size={16} />
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}

          {/* ── Step 3: Review ── */}
          {step === 3 && (
            <div className="space-y-5">
              <h2 className="text-lg font-semibold text-gray-800">Review & Submit</h2>

              <Section title="Loan Details">
                <Row label="Customer Name"    value={info.customerName} />
                <Row label="Account Number"   value={info.accountNumber} />
                <Row label="BVN"              value={info.bvnNumber} />
                <Row label="Loan Type"        value={info.loanType} />
                <Row label="Loan Amount"      value={`₦${Number(info.loanAmount).toLocaleString()}`} />
                <Row label="Tenor"            value={`${info.loanTenor} months`} />
                <Row label="Purpose"          value={info.loanPurpose} />
              </Section>

              <Section title="Financial Information">
                <Row label="Monthly Income"   value={`₦${Number(info.monthlyIncome).toLocaleString()}`} />
                <Row label="Employer"         value={info.employerName} />
                <Row label="Collateral"       value={info.collateralType} />
                {info.collateralType !== 'None' && info.collateralValue && (
                  <Row label="Collateral Value" value={`₦${Number(info.collateralValue).toLocaleString()}`} />
                )}
              </Section>

              <Section title="Documents">
                {files.length === 0
                  ? <Row label="Files" value="None attached" />
                  : files.map((f, i) => <Row key={i} label={`File ${i + 1}`} value={f.name} />)
                }
              </Section>

              {errors.submit && (
                <div className="rounded-lg bg-red-50 border border-red-300 text-red-700 text-sm px-4 py-3">
                  {errors.submit}
                </div>
              )}

              {submitting && (
                <div className="flex items-center gap-3 text-blue-600 text-sm">
                  <Loader2 size={16} className="animate-spin" />
                  {progress}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Navigation */}
        <div className="flex items-center justify-between mt-6">
          <button
            onClick={() => step === 0 ? navigate('/forms') : back()}
            disabled={submitting}
            className="flex items-center gap-2 px-4 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 transition-colors"
          >
            <ChevronLeft size={16} />
            {step === 0 ? 'Cancel' : 'Back'}
          </button>

          {step < STEPS.length - 1 ? (
            <button
              onClick={next}
              className="flex items-center gap-2 px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
            >
              Next <ChevronRight size={16} />
            </button>
          ) : (
            <button
              onClick={handleSubmit}
              disabled={submitting}
              className="flex items-center gap-2 px-5 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-sm font-medium rounded-lg transition-colors"
            >
              {submitting ? <Loader2 size={16} className="animate-spin" /> : <CheckCircle size={16} />}
              {submitting ? 'Submitting…' : 'Submit for Approval'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Sub-components ─────────────────────────────────────────────────────────────

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      {children}
      {error && <p className="text-xs text-red-600 mt-1">{error}</p>}
    </div>
  );
}

function cls(error?: string) {
  return `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors ${
    error ? 'border-red-400 bg-red-50' : 'border-gray-300 bg-white'
  }`;
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-gray-200 overflow-hidden">
      <div className="bg-gray-50 px-4 py-2 border-b border-gray-200">
        <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wide">{title}</h3>
      </div>
      <div className="divide-y divide-gray-100">{children}</div>
    </div>
  );
}

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex gap-4 px-4 py-2.5">
      <span className="text-sm text-gray-500 w-40 flex-shrink-0">{label}</span>
      <span className="text-sm text-gray-900 font-medium break-all">{value}</span>
    </div>
  );
}
