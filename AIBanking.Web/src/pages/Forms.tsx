import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FileInput, ChevronRight, CheckCircle2 } from 'lucide-react';
import { formsApi } from '../api/forms';
import { workflowsApi } from '../api/workflows';
import type { FormDefinition } from '../types';
import DynamicForm from '../components/DynamicForm';
import { getAxiosErrorMessage } from '../utils/errorHandling';

// Forms with dedicated multi-step pages (redirect instead of DynamicForm)
const DEDICATED_ROUTES: Record<string, string> = {
  '33333333-0000-0000-0000-000000000001': '/applications/new', // Account Opening
  '33333333-0000-0000-0000-000000000002': '/loans/new',        // Loan Booking
};

// ── Form list ─────────────────────────────────────────────────────────────────

function FormList({
  forms,
  onSelect,
}: {
  forms: FormDefinition[];
  onSelect: (f: FormDefinition) => void;
}) {
  return (
    <div className="space-y-3">
      {forms.map(form => (
        <button
          key={form.id}
          onClick={() => onSelect(form)}
          className="w-full text-left bg-white border border-gray-200 rounded-xl p-4 hover:border-blue-400 hover:shadow-sm transition-all group"
        >
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-50 rounded-lg group-hover:bg-blue-100 transition-colors">
                <FileInput size={18} className="text-blue-600" />
              </div>
              <div>
                <p className="font-medium text-gray-900">{form.name}</p>
                {form.description && (
                  <p className="text-sm text-gray-500 mt-0.5">{form.description}</p>
                )}
              </div>
            </div>
            <div className="flex items-center gap-3">
              {form.workflowDefinition && (
                <span className="hidden sm:flex items-center gap-1 text-xs text-gray-400">
                  {form.workflowDefinition.stages.map((s, i) => (
                    <span key={s.id} className="flex items-center gap-1">
                      {i > 0 && <ChevronRight size={10} />}
                      {s.stageName}
                    </span>
                  ))}
                </span>
              )}
              <ChevronRight size={16} className="text-gray-400 group-hover:text-blue-500 transition-colors" />
            </div>
          </div>
        </button>
      ))}
    </div>
  );
}

// ── Success screen ────────────────────────────────────────────────────────────

function SuccessScreen({ workflowId, onReset }: { workflowId: string; onReset: () => void }) {
  const navigate = useNavigate();
  return (
    <div className="text-center py-12">
      <div className="inline-flex items-center justify-center w-16 h-16 bg-green-100 rounded-full mb-4">
        <CheckCircle2 size={32} className="text-green-600" />
      </div>
      <h2 className="text-xl font-semibold text-gray-900 mb-2">Form submitted</h2>
      <p className="text-sm text-gray-500 mb-6">
        Your request has been sent for approval and is now pending review.
      </p>
      <div className="flex gap-3 justify-center">
        <button
          onClick={() => navigate(`/workflows/${workflowId}`)}
          className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
        >
          View workflow
        </button>
        <button
          onClick={onReset}
          className="px-4 py-2 border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors"
        >
          Submit another
        </button>
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function Forms() {
  const navigate                    = useNavigate();
  const [forms, setForms]           = useState<FormDefinition[]>([]);
  const [selected, setSelected]     = useState<FormDefinition | null>(null);
  const [loading, setLoading]       = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError]           = useState('');
  const [successId, setSuccessId]   = useState<string | null>(null);

  useEffect(() => {
    formsApi.listDefinitions()
      .then(setForms)
      .finally(() => setLoading(false));
  }, []);

  function handleSelect(form: FormDefinition) {
    const route = DEDICATED_ROUTES[form.id];
    if (route) { navigate(route); return; }
    setSelected(form);
  }

  async function handleSubmit(values: Record<string, string>, files: Record<string, File>) {
    if (!selected) return;
    setError('');
    setSubmitting(true);
    try {
      const result = await formsApi.submit(selected.id, values);
      const workflowId = (result.workflowItem as { id: string }).id;

      // Upload any file attachments to the created workflow
      for (const file of Object.values(files)) {
        await workflowsApi.uploadDocument(workflowId, file);
      }

      setSuccessId(workflowId);
    } catch (err: unknown) {
      setError(getAxiosErrorMessage(err, 'Submission failed. Please try again.'));
    } finally {
      setSubmitting(false);
    }
  }

  function reset() {
    setSelected(null);
    setSuccessId(null);
    setError('');
  }

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Forms</h1>
        <p className="text-sm text-gray-500 mt-0.5">
          {selected ? selected.name : 'Select a form to fill out'}
        </p>
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400 text-sm">Loading…</div>
      ) : successId ? (
        <SuccessScreen workflowId={successId} onReset={reset} />
      ) : selected ? (
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <div className="flex items-center gap-2 mb-6">
            <button
              onClick={() => setSelected(null)}
              className="text-sm text-blue-600 hover:underline"
            >
              ← Back
            </button>
            <span className="text-gray-300">/</span>
            <span className="text-sm text-gray-600">{selected.name}</span>
          </div>

          {selected.workflowDefinition && (
            <div className="flex items-center gap-1.5 mb-6 text-xs text-gray-400 flex-wrap">
              <span className="font-medium text-gray-500">Approval chain:</span>
              {selected.workflowDefinition.stages.map((s, i) => (
                <span key={s.id} className="flex items-center gap-1">
                  {i > 0 && <ChevronRight size={10} />}
                  <span className="bg-gray-100 px-2 py-0.5 rounded-full">{s.stageName}</span>
                </span>
              ))}
            </div>
          )}

          {error && (
            <div className="mb-4 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">
              {error}
            </div>
          )}

          <DynamicForm
            definition={selected}
            onSubmit={handleSubmit}
            loading={submitting}
          />
        </div>
      ) : forms.length === 0 ? (
        <div className="text-center py-12 text-gray-400 text-sm">No forms available.</div>
      ) : (
        <FormList forms={forms} onSelect={handleSelect} />
      )}
    </div>
  );
}
