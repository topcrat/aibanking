import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft, CheckCircle2, RotateCcw, XCircle,
  ChevronRight, User, Calendar, AlertTriangle, FileText, Paperclip, Download,
} from 'lucide-react';
import { workflowsApi } from '../api/workflows';
import { formsApi } from '../api/forms';
import type { WorkflowItem, WorkflowDocument, FormSubmission, WorkflowApproval } from '../types';
import { WorkflowStatusBadge } from '../components/StatusBadge';
import { getAxiosErrorMessage } from '../utils/errorHandling';

// ── Pipeline progress bar ─────────────────────────────────────────────────────

function PipelineProgress({ item }: { item: WorkflowItem }) {
  const stages = item.definition?.stages ?? [];
  const isDone  = item.status === 'Approved' || item.status === 'Declined';

  return (
    <div className="flex items-center gap-0">
      {stages.map((stage, i) => {
        const pastStage    = item.currentStageOrder > stage.stageOrder;
        const activeStage  = item.currentStageOrder === stage.stageOrder && !isDone;
        const approvedFull = item.status === 'Approved';

        const dot = pastStage || approvedFull
          ? 'bg-green-500 border-green-500 text-white'
          : activeStage
            ? item.status === 'Rework'
              ? 'bg-orange-500 border-orange-500 text-white'
              : 'bg-blue-600 border-blue-600 text-white'
            : 'bg-white border-gray-300 text-gray-400';

        const line = (pastStage || approvedFull) ? 'bg-green-400' : 'bg-gray-200';

        return (
          <div key={stage.id} className="flex items-center flex-1 min-w-0">
            <div className="flex flex-col items-center shrink-0">
              <div className={`w-8 h-8 rounded-full border-2 flex items-center justify-center text-xs font-bold transition-colors ${dot}`}>
                {pastStage || approvedFull
                  ? <CheckCircle2 size={14} />
                  : stage.stageOrder}
              </div>
              <p className="text-xs text-gray-500 mt-1 text-center whitespace-nowrap max-w-[80px] truncate">
                {stage.stageName}
              </p>
              <p className="text-[10px] text-gray-400 text-center">{stage.requiredRole}</p>
            </div>
            {i < stages.length - 1 && (
              <div className={`flex-1 h-0.5 mx-1 mb-5 transition-colors ${line}`} />
            )}
          </div>
        );
      })}
    </div>
  );
}

// ── Form data viewer ──────────────────────────────────────────────────────────

function FormDataPanel({ submission }: { submission: FormSubmission }) {
  const values: Record<string, string> = JSON.parse(submission.valuesJson || '{}');
  const fields = submission.formDefinition?.fields ?? [];

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-gray-700 flex items-center gap-2">
        <FileText size={14} />
        {submission.formDefinition?.name ?? 'Form Data'}
      </h3>
      <div className="grid grid-cols-1 gap-3">
        {fields.map(field => {
          const value = values[field.fieldKey];
          return (
            <div key={field.id} className="flex flex-col gap-0.5">
              <span className="text-xs font-medium text-gray-500">{field.label}</span>
              <span className={`text-sm ${value ? 'text-gray-900' : 'text-gray-300 italic'}`}>
                {field.fieldType === 'File'
                  ? value
                    ? <span className="inline-flex items-center gap-1 text-blue-600"><FileText size={12} />{value}</span>
                    : '—'
                  : value || '—'}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ── Approval history timeline ─────────────────────────────────────────────────

function ApprovalTimeline({ approvals }: { approvals: WorkflowApproval[] }) {
  if (approvals.length === 0)
    return <p className="text-sm text-gray-400 italic">No actions taken yet.</p>;

  const colors: Record<string, string> = {
    Approved: 'bg-green-100 text-green-700',
    Rework:   'bg-orange-100 text-orange-700',
    Declined: 'bg-red-100 text-red-700',
  };

  return (
    <div className="space-y-3">
      {approvals.map(a => (
        <div key={a.id} className="flex gap-3">
          <div className="flex flex-col items-center">
            <div className="w-7 h-7 rounded-full bg-gray-100 flex items-center justify-center shrink-0">
              {a.action === 'Approved' ? <CheckCircle2 size={14} className="text-green-600" />
                : a.action === 'Rework' ? <RotateCcw size={14} className="text-orange-500" />
                : <XCircle size={14} className="text-red-500" />}
            </div>
            <div className="flex-1 w-px bg-gray-200 my-1" />
          </div>
          <div className="pb-3 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <span className={`text-xs font-medium px-1.5 py-0.5 rounded ${colors[a.action]}`}>
                {a.action}
              </span>
              <span className="text-xs text-gray-500">{a.stageName}</span>
              <span className="text-xs text-gray-400">
                {new Date(a.actedAt).toLocaleString()}
              </span>
            </div>
            <p className="text-sm font-medium text-gray-800 mt-0.5">{a.actedBy}</p>
            {a.comments && (
              <p className="text-sm text-gray-500 mt-0.5 italic">"{a.comments}"</p>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}

// ── Action panel ──────────────────────────────────────────────────────────────

type ActionType = 'approve' | 'rework' | 'decline';

function ActionPanel({
  item,
  onAct,
  acting,
}: {
  item: WorkflowItem;
  onAct: (action: ActionType, comments: string) => Promise<void>;
  acting: boolean;
}) {
  const [chosen,   setChosen]   = useState<ActionType | null>(null);
  const [comments, setComments] = useState('');

  const userRole    = localStorage.getItem('role') ?? '';
  const currentStage = item.definition?.stages.find(s => s.stageOrder === item.currentStageOrder);
  const canAct      = item.status === 'Pending'
    && currentStage
    && (userRole === 'Admin' || userRole === currentStage.requiredRole);

  const canResubmit = item.status === 'Rework'
    && (item.submittedBy === (localStorage.getItem('username') ?? '') || userRole === 'Admin');

  if (!canAct && !canResubmit) {
    return (
      <div className="text-sm text-gray-400 italic text-center py-4">
        {item.status === 'Approved' || item.status === 'Declined'
          ? 'This workflow is closed.'
          : 'You do not have permission to act on this stage.'}
      </div>
    );
  }

  async function confirm() {
    if (!chosen) return;
    if (chosen !== 'approve' && !comments.trim()) return;
    await onAct(chosen, comments);
    setChosen(null);
    setComments('');
  }

  const actionConfig = {
    approve: { label: 'Approve',  color: 'bg-green-600 hover:bg-green-700', icon: <CheckCircle2 size={14} /> },
    rework:  { label: 'Rework',   color: 'bg-orange-500 hover:bg-orange-600', icon: <RotateCcw size={14} /> },
    decline: { label: 'Decline',  color: 'bg-red-600 hover:bg-red-700', icon: <XCircle size={14} /> },
  };

  return (
    <div className="space-y-4">
      {canAct && currentStage && (
        <>
          <div className="text-xs text-gray-500 bg-blue-50 rounded-lg px-3 py-2">
            Acting as <strong>{userRole}</strong> on stage <strong>{currentStage.stageName}</strong>
          </div>

          {!chosen ? (
            <div className="flex flex-col gap-2">
              {(['approve', 'rework', 'decline'] as ActionType[]).map(a => (
                <button
                  key={a}
                  onClick={() => setChosen(a)}
                  className={`flex items-center justify-center gap-2 w-full py-2 text-sm font-medium text-white rounded-lg transition-colors ${actionConfig[a].color}`}
                >
                  {actionConfig[a].icon}
                  {actionConfig[a].label}
                </button>
              ))}
            </div>
          ) : (
            <div className="space-y-3">
              <p className="text-sm font-medium text-gray-700">
                {actionConfig[chosen].label} — add comments
                {chosen !== 'approve' && <span className="text-red-500"> *</span>}
              </p>
              <textarea
                rows={3}
                value={comments}
                onChange={e => setComments(e.target.value)}
                placeholder={
                  chosen === 'rework'  ? 'Describe what needs correction…'
                  : chosen === 'decline' ? 'Reason for declining…'
                  : 'Optional comments…'
                }
                className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              />
              <div className="flex gap-2">
                <button
                  onClick={() => { setChosen(null); setComments(''); }}
                  className="flex-1 py-2 border border-gray-300 text-gray-700 text-sm rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Back
                </button>
                <button
                  onClick={confirm}
                  disabled={acting || (chosen !== 'approve' && !comments.trim())}
                  className={`flex-1 py-2 text-white text-sm font-medium rounded-lg transition-colors disabled:opacity-50 ${actionConfig[chosen].color}`}
                >
                  {acting ? 'Saving…' : 'Confirm'}
                </button>
              </div>
            </div>
          )}
        </>
      )}

      {canResubmit && (
        <button
          onClick={() => onAct('approve', '')} // triggers resubmit path in parent
          disabled={acting}
          className="w-full py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors"
        >
          {acting ? 'Resubmitting…' : 'Resubmit for review'}
        </button>
      )}
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function WorkflowReview() {
  const { id }     = useParams<{ id: string }>();
  const navigate   = useNavigate();

  const [item,      setItem]      = useState<WorkflowItem | null>(null);
  const [documents, setDocuments] = useState<WorkflowDocument[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [acting,    setActing]    = useState(false);
  const [error,     setError]     = useState('');

  async function load() {
    if (!id) return;
    setLoading(true);
    try {
      const [wf, docs, sub] = await Promise.allSettled([
        workflowsApi.getById(id),
        workflowsApi.listDocuments(id),
        formsApi.getSubmissionByWorkflow(id),
      ]);
      if (wf.status === 'fulfilled') {
        const workflowItem = wf.value;
        const formSubmission = sub.status === 'fulfilled' ? sub.value : workflowItem.formSubmission;
        setItem({ ...workflowItem, formSubmission: formSubmission ?? workflowItem.formSubmission });
      }
      if (docs.status === 'fulfilled') setDocuments(docs.value as WorkflowDocument[]);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, [id]); // eslint-disable-line react-hooks/exhaustive-deps

  async function handleAct(action: ActionType | 'resubmit', comments: string) {
    if (!id) return;
    setError('');
    setActing(true);
    try {
      if (action === 'resubmit' || (item?.status === 'Rework' && action === 'approve')) {
        await workflowsApi.resubmit(id);
      } else if (action === 'approve') {
        await workflowsApi.approve(id, { comments });
      } else if (action === 'rework') {
        await workflowsApi.rework(id, { comments });
      } else if (action === 'decline') {
        await workflowsApi.decline(id, { comments });
      }
      await load();
    } catch (err: unknown) {
      setError(getAxiosErrorMessage(err, 'Action failed. Please try again.'));
    } finally {
      setActing(false);
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full" />
      </div>
    );
  }

  if (!item) {
    return (
      <div className="p-6 text-center text-gray-500">Workflow not found.</div>
    );
  }

  const approvals = item.approvals ?? [];

  return (
    <div className="p-6 max-w-6xl mx-auto">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-400 mb-5">
        <button onClick={() => navigate('/workflows')} className="hover:text-gray-600 flex items-center gap-1">
          <ArrowLeft size={14} /> Workflows
        </button>
        <ChevronRight size={12} />
        <span className="text-gray-700 font-medium truncate max-w-xs">{item.title}</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between gap-4 mb-6 flex-wrap">
        <div>
          <h1 className="text-xl font-bold text-gray-900">{item.title}</h1>
          <div className="flex items-center gap-3 mt-1 text-sm text-gray-500 flex-wrap">
            <span className="flex items-center gap-1"><User size={12} /> {item.submittedBy}</span>
            <span className="flex items-center gap-1"><Calendar size={12} /> {new Date(item.createdAt).toLocaleString()}</span>
          </div>
        </div>
        <WorkflowStatusBadge status={item.status} />
      </div>

      {/* Pipeline progress */}
      {item.definition && (
        <div className="bg-white rounded-xl border border-gray-200 p-5 mb-6">
          <p className="text-xs font-semibold text-gray-400 uppercase mb-4">Approval pipeline — {item.definition.name}</p>
          <PipelineProgress item={item} />
        </div>
      )}

      {/* Rework banner */}
      {item.status === 'Rework' && item.comments && (
        <div className="flex items-start gap-3 bg-orange-50 border border-orange-200 rounded-xl p-4 mb-6 text-sm text-orange-800">
          <AlertTriangle size={16} className="shrink-0 mt-0.5" />
          <div>
            <p className="font-medium mb-0.5">Returned for rework</p>
            <p>"{item.comments}"</p>
          </div>
        </div>
      )}

      {error && (
        <div className="mb-4 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">
          {error}
        </div>
      )}

      {/* Main content: form data + actions */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left: form data */}
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white rounded-xl border border-gray-200 p-5">
            {item.formSubmission ? (
              <FormDataPanel submission={item.formSubmission as FormSubmission} />
            ) : (
              <div className="text-sm text-gray-400 italic">
                No form data linked to this workflow.
              </div>
            )}
          </div>

          {/* Attachments */}
          {documents.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-200 p-5">
              <h3 className="text-sm font-semibold text-gray-700 mb-3 flex items-center gap-2">
                <Paperclip size={14} /> Attachments ({documents.length})
              </h3>
              <ul className="space-y-2">
                {documents.map(doc => (
                  <li key={doc.id} className="flex items-center justify-between gap-3 px-3 py-2 bg-gray-50 rounded-lg border border-gray-200">
                    <div className="flex items-center gap-2 min-w-0">
                      <FileText size={14} className="text-gray-400 shrink-0" />
                      <span className="text-sm text-gray-800 truncate">{doc.fileName}</span>
                    </div>
                    <a
                      href={`/api/workflow/${doc.workflowId}/documents/${doc.id}/download`}
                      target="_blank"
                      rel="noreferrer"
                      className="shrink-0 text-blue-600 hover:text-blue-700 transition-colors"
                      title="Download"
                    >
                      <Download size={15} />
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Approval history */}
          <div className="bg-white rounded-xl border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-700 mb-4">Approval history</h3>
            <ApprovalTimeline approvals={approvals} />
          </div>
        </div>

        {/* Right: action panel */}
        <div className="space-y-4">
          <div className="bg-white rounded-xl border border-gray-200 p-5 sticky top-6">
            <h3 className="text-sm font-semibold text-gray-700 mb-4">Actions</h3>
            <ActionPanel item={item} onAct={handleAct} acting={acting} />
          </div>

          {/* Metadata */}
          <div className="bg-white rounded-xl border border-gray-200 p-5 text-sm space-y-2">
            <div className="flex justify-between">
              <span className="text-gray-500">Reviewed by</span>
              <span className="text-gray-800">{item.reviewedBy ?? '—'}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-500">Last updated</span>
              <span className="text-gray-800">{new Date(item.updatedAt).toLocaleString()}</span>
            </div>
            {item.definition && (
              <div className="flex justify-between">
                <span className="text-gray-500">Pipeline</span>
                <span className="text-gray-800">{item.definition.name}</span>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
