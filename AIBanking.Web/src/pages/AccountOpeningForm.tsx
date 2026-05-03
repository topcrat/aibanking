import { useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { Upload, X, CheckCircle, ChevronRight, ChevronLeft, Loader2 } from 'lucide-react';
import { applicationsApi } from '../api/applications';

// ── Types ────────────────────────────────────────────────────────────────────

interface PersonalInfo {
  fullName: string;
  dateOfBirth: string;
  gender: 'Male' | 'Female' | 'Other' | '';
  phoneNumber: string;
  residenceAddress: string;
  bvnNumber: string;
  ninNumber: string;
  consentGiven: boolean;
}

interface DocFiles {
  identityCard: File | null;
  accountOpeningForm: File | null;
}

const ACCEPTED = 'image/jpeg,image/png,image/webp,application/pdf';
const STEPS = ['Personal Info', 'Documents', 'Review & Submit'];

// ── Component ────────────────────────────────────────────────────────────────

export default function AccountOpeningForm() {
  const navigate = useNavigate();
  const [step, setStep] = useState(0);
  const [info, setInfo] = useState<PersonalInfo>({
    fullName: '',
    dateOfBirth: '',
    gender: '',
    phoneNumber: '',
    residenceAddress: '',
    bvnNumber: '',
    ninNumber: '',
    consentGiven: false,
  });
  const [docs, setDocs] = useState<DocFiles>({ identityCard: null, accountOpeningForm: null });
  const [errors, setErrors] = useState<Partial<Record<keyof PersonalInfo | 'identityCard' | 'accountOpeningForm' | 'submit', string>>>({});
  const [submitting, setSubmitting] = useState(false);
  const [progress, setProgress] = useState('');
  const idCardRef = useRef<HTMLInputElement>(null);
  const aofRef = useRef<HTMLInputElement>(null);

  // ── Validation ─────────────────────────────────────────────────────────────

  function validateStep0(): boolean {
    const e: typeof errors = {};
    if (!info.fullName.trim()) e.fullName = 'Full name is required.';
    if (!info.dateOfBirth) e.dateOfBirth = 'Date of birth is required.';
    if (!info.gender) e.gender = 'Gender is required.';
    if (!info.phoneNumber.trim()) e.phoneNumber = 'Phone number is required.';
    if (!info.residenceAddress.trim()) e.residenceAddress = 'Address is required.';
    if (!/^\d{11}$/.test(info.bvnNumber)) e.bvnNumber = 'BVN must be exactly 11 digits.';
    if (info.ninNumber && !/^\d{11}$/.test(info.ninNumber)) e.ninNumber = 'NIN must be exactly 11 digits.';
    if (!info.consentGiven) e.consentGiven = 'You must accept the NDPA consent.';
    setErrors(e);
    return Object.keys(e).length === 0;
  }

  function validateStep1(): boolean {
    const e: typeof errors = {};
    if (!docs.identityCard) e.identityCard = 'Identity card is required.';
    if (!docs.accountOpeningForm) e.accountOpeningForm = 'Account opening form document is required.';
    setErrors(e);
    return Object.keys(e).length === 0;
  }

  // ── Navigation ─────────────────────────────────────────────────────────────

  function next() {
    if (step === 0 && !validateStep0()) return;
    if (step === 1 && !validateStep1()) return;
    setStep(s => s + 1);
  }

  function back() {
    setErrors({});
    setStep(s => s - 1);
  }

  // ── File handling ──────────────────────────────────────────────────────────

  function handleFile(field: keyof DocFiles, file: File | undefined) {
    if (!file) return;
    setDocs(d => ({ ...d, [field]: file }));
    setErrors(e => ({ ...e, [field]: undefined }));
  }

  function removeFile(field: keyof DocFiles) {
    setDocs(d => ({ ...d, [field]: null }));
  }

  // ── Submit ─────────────────────────────────────────────────────────────────

  async function handleSubmit() {
    setSubmitting(true);
    setErrors({});
    try {
      setProgress('Creating application…');
      const app = await applicationsApi.create();

      setProgress('Uploading identity card…');
      await applicationsApi.uploadDocument(app.id, docs.identityCard!, 'IdentityCard');

      setProgress('Uploading account opening form…');
      await applicationsApi.uploadDocument(app.id, docs.accountOpeningForm!, 'AccountOpeningForm');

      navigate(`/applications/${app.id}`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Submission failed. Please try again.';
      setErrors({ submit: msg });
      setSubmitting(false);
      setProgress('');
    }
  }

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="min-h-full p-8 flex flex-col items-center">
      <div className="w-full max-w-2xl">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-gray-900">New Account Opening</h1>
          <p className="text-gray-500 mt-1">Complete the form to submit your account application.</p>
        </div>

        {/* Stepper */}
        <div className="flex items-center gap-0 mb-8">
          {STEPS.map((label, i) => (
            <div key={i} className="flex items-center flex-1 last:flex-none">
              <div className={`flex items-center gap-2 ${i <= step ? 'text-blue-600' : 'text-gray-400'}`}>
                <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-semibold border-2 transition-colors
                  ${i < step ? 'bg-blue-600 border-blue-600 text-white'
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

          {/* ── Step 0: Personal Information ── */}
          {step === 0 && (
            <div className="space-y-5">
              <h2 className="text-lg font-semibold text-gray-800">Personal Information</h2>

              <Field label="Full Name *" error={errors.fullName}>
                <input
                  type="text"
                  value={info.fullName}
                  onChange={e => setInfo(i => ({ ...i, fullName: e.target.value }))}
                  placeholder="As it appears on your ID"
                  className={inputCls(errors.fullName)}
                />
              </Field>

              <div className="grid grid-cols-2 gap-4">
                <Field label="Date of Birth *" error={errors.dateOfBirth}>
                  <input
                    type="date"
                    value={info.dateOfBirth}
                    max={new Date().toISOString().slice(0, 10)}
                    onChange={e => setInfo(i => ({ ...i, dateOfBirth: e.target.value }))}
                    className={inputCls(errors.dateOfBirth)}
                  />
                </Field>

                <Field label="Gender *" error={errors.gender}>
                  <select
                    value={info.gender}
                    onChange={e => setInfo(i => ({ ...i, gender: e.target.value as PersonalInfo['gender'] }))}
                    className={inputCls(errors.gender)}
                  >
                    <option value="">Select…</option>
                    <option value="Male">Male</option>
                    <option value="Female">Female</option>
                    <option value="Other">Other</option>
                  </select>
                </Field>
              </div>

              <Field label="Phone Number *" error={errors.phoneNumber}>
                <input
                  type="tel"
                  value={info.phoneNumber}
                  onChange={e => setInfo(i => ({ ...i, phoneNumber: e.target.value }))}
                  placeholder="+234…"
                  className={inputCls(errors.phoneNumber)}
                />
              </Field>

              <Field label="Residence Address *" error={errors.residenceAddress}>
                <textarea
                  value={info.residenceAddress}
                  onChange={e => setInfo(i => ({ ...i, residenceAddress: e.target.value }))}
                  rows={2}
                  placeholder="Street, city, state"
                  className={inputCls(errors.residenceAddress)}
                />
              </Field>

              <div className="grid grid-cols-2 gap-4">
                <Field label="BVN (11 digits) *" error={errors.bvnNumber}>
                  <input
                    type="text"
                    inputMode="numeric"
                    maxLength={11}
                    value={info.bvnNumber}
                    onChange={e => setInfo(i => ({ ...i, bvnNumber: e.target.value.replace(/\D/g, '') }))}
                    placeholder="22200000000"
                    className={inputCls(errors.bvnNumber)}
                  />
                </Field>

                <Field label="NIN (optional)" error={errors.ninNumber}>
                  <input
                    type="text"
                    inputMode="numeric"
                    maxLength={11}
                    value={info.ninNumber}
                    onChange={e => setInfo(i => ({ ...i, ninNumber: e.target.value.replace(/\D/g, '') }))}
                    placeholder="12345678901"
                    className={inputCls(errors.ninNumber)}
                  />
                </Field>
              </div>

              <div className={`rounded-lg border p-4 ${errors.consentGiven ? 'border-red-400 bg-red-50' : 'border-gray-200 bg-gray-50'}`}>
                <label className="flex items-start gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={info.consentGiven}
                    onChange={e => setInfo(i => ({ ...i, consentGiven: e.target.checked }))}
                    className="mt-0.5 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="text-sm text-gray-700">
                    I consent to the processing of my personal data in accordance with the{' '}
                    <strong>Nigeria Data Protection Act (NDPA)</strong> for the purpose of
                    account opening and related banking services.
                  </span>
                </label>
                {errors.consentGiven && <p className="text-xs text-red-600 mt-2">{errors.consentGiven}</p>}
              </div>
            </div>
          )}

          {/* ── Step 1: Document Uploads ── */}
          {step === 1 && (
            <div className="space-y-6">
              <h2 className="text-lg font-semibold text-gray-800">Upload Documents</h2>
              <p className="text-sm text-gray-500">Accepted formats: JPEG, PNG, WebP, PDF. Max 10 MB per file.</p>

              <UploadSlot
                label="Identity Card *"
                description="National ID, Driver's Licence, Voter's Card, or International Passport"
                file={docs.identityCard}
                error={errors.identityCard}
                inputRef={idCardRef}
                accept={ACCEPTED}
                onSelect={f => handleFile('identityCard', f)}
                onRemove={() => removeFile('identityCard')}
              />

              <UploadSlot
                label="Account Opening Form *"
                description="Signed account opening form (scanned copy or photo)"
                file={docs.accountOpeningForm}
                error={errors.accountOpeningForm}
                inputRef={aofRef}
                accept={ACCEPTED}
                onSelect={f => handleFile('accountOpeningForm', f)}
                onRemove={() => removeFile('accountOpeningForm')}
              />
            </div>
          )}

          {/* ── Step 2: Review ── */}
          {step === 2 && (
            <div className="space-y-5">
              <h2 className="text-lg font-semibold text-gray-800">Review & Submit</h2>

              <Section title="Personal Information">
                <Row label="Full Name" value={info.fullName} />
                <Row label="Date of Birth" value={info.dateOfBirth} />
                <Row label="Gender" value={info.gender} />
                <Row label="Phone" value={info.phoneNumber} />
                <Row label="Address" value={info.residenceAddress} />
                <Row label="BVN" value={info.bvnNumber} />
                {info.ninNumber && <Row label="NIN" value={info.ninNumber} />}
                <Row label="NDPA Consent" value={info.consentGiven ? 'Given' : 'Not given'} />
              </Section>

              <Section title="Documents">
                <Row label="Identity Card" value={docs.identityCard?.name ?? '—'} />
                <Row label="Account Opening Form" value={docs.accountOpeningForm?.name ?? '—'} />
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

        {/* Navigation Buttons */}
        <div className="flex items-center justify-between mt-6">
          <button
            onClick={() => step === 0 ? navigate('/applications') : back()}
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
              Next
              <ChevronRight size={16} />
            </button>
          ) : (
            <button
              onClick={handleSubmit}
              disabled={submitting}
              className="flex items-center gap-2 px-5 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-sm font-medium rounded-lg transition-colors"
            >
              {submitting ? <Loader2 size={16} className="animate-spin" /> : <CheckCircle size={16} />}
              {submitting ? 'Submitting…' : 'Submit Application'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

// ── Sub-components ────────────────────────────────────────────────────────────

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      {children}
      {error && <p className="text-xs text-red-600 mt-1">{error}</p>}
    </div>
  );
}

function inputCls(error?: string) {
  return `w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors ${
    error ? 'border-red-400 bg-red-50' : 'border-gray-300 bg-white'
  }`;
}

interface UploadSlotProps {
  label: string;
  description: string;
  file: File | null;
  error?: string;
  inputRef: React.RefObject<HTMLInputElement | null>;
  accept: string;
  onSelect: (f: File) => void;
  onRemove: () => void;
}

function UploadSlot({ label, description, file, error, inputRef, accept, onSelect, onRemove }: UploadSlotProps) {
  function handleDrop(e: React.DragEvent) {
    e.preventDefault();
    const f = e.dataTransfer.files[0];
    if (f) onSelect(f);
  }

  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-2">{label}</label>
      <p className="text-xs text-gray-500 mb-2">{description}</p>

      {file ? (
        <div className="flex items-center gap-3 px-4 py-3 border border-blue-300 bg-blue-50 rounded-lg">
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-blue-800 truncate">{file.name}</p>
            <p className="text-xs text-blue-600">{(file.size / 1024).toFixed(0)} KB</p>
          </div>
          <button
            onClick={onRemove}
            className="text-blue-500 hover:text-red-500 transition-colors"
            title="Remove file"
          >
            <X size={16} />
          </button>
        </div>
      ) : (
        <div
          onDragOver={e => e.preventDefault()}
          onDrop={handleDrop}
          onClick={() => inputRef.current?.click()}
          className={`border-2 border-dashed rounded-lg px-6 py-8 text-center cursor-pointer transition-colors hover:border-blue-400 hover:bg-blue-50 ${
            error ? 'border-red-400 bg-red-50' : 'border-gray-300 bg-gray-50'
          }`}
        >
          <Upload size={24} className={`mx-auto mb-2 ${error ? 'text-red-400' : 'text-gray-400'}`} />
          <p className="text-sm text-gray-600">
            <span className="font-medium text-blue-600">Click to browse</span> or drag & drop
          </p>
          <p className="text-xs text-gray-400 mt-1">JPEG, PNG, WebP, PDF</p>
        </div>
      )}

      <input
        ref={inputRef}
        type="file"
        accept={accept}
        className="hidden"
        onChange={e => { const f = e.target.files?.[0]; if (f) onSelect(f); e.target.value = ''; }}
      />

      {error && <p className="text-xs text-red-600 mt-1">{error}</p>}
    </div>
  );
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
