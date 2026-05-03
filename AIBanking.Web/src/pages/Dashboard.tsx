import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FileText, GitBranch, CheckCircle, Clock, AlertTriangle, Bot, CreditCard, Plus } from 'lucide-react';
import { applicationsApi } from '../api/applications';
import { workflowsApi } from '../api/workflows';
import type { AccountApplication, WorkflowItem } from '../types';
import { AppStatusBadge } from '../components/StatusBadge';

export default function Dashboard() {
  const navigate = useNavigate();
  const [apps,      setApps]      = useState<AccountApplication[]>([]);
  const [workflows, setWorkflows] = useState<WorkflowItem[]>([]);
  const [loading,   setLoading]   = useState(true);

  useEffect(() => {
    Promise.all([applicationsApi.list(), workflowsApi.list()])
      .then(([a, w]) => { setApps(a); setWorkflows(w); })
      .finally(() => setLoading(false));
  }, []);

  const appsByStatus = {
    Active:      apps.filter(a => a.status === 'Active').length,
    UnderReview: apps.filter(a => a.status === 'UnderReview').length,
    Rework:      apps.filter(a => a.status === 'Rework').length,
    Rejected:    apps.filter(a => a.status === 'Rejected').length,
  };
  const pendingWorkflows = workflows.filter(w => w.status === 'Pending').length;

  const recent = [...apps].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  ).slice(0, 6);

  const stats = [
    { label: 'Total Applications', value: apps.length,           icon: FileText,     color: 'blue' },
    { label: 'Active Accounts',    value: appsByStatus.Active,   icon: CheckCircle,  color: 'green' },
    { label: 'Under Review',       value: appsByStatus.UnderReview, icon: Clock,     color: 'yellow' },
    { label: 'Pending Workflows',  value: pendingWorkflows,      icon: GitBranch,    color: 'purple' },
    { label: 'Requires Rework',    value: appsByStatus.Rework,   icon: AlertTriangle, color: 'orange' },
  ];

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="animate-spin w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full" />
      </div>
    );
  }

  return (
    <div className="p-8">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-500 mt-1">Welcome back. Here's what's happening today.</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-8">
        {stats.map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
            <div className={`inline-flex p-2 rounded-lg mb-3 ${
              color === 'blue'   ? 'bg-blue-100 text-blue-600' :
              color === 'green'  ? 'bg-green-100 text-green-600' :
              color === 'yellow' ? 'bg-yellow-100 text-yellow-600' :
              color === 'purple' ? 'bg-purple-100 text-purple-600' :
                                   'bg-orange-100 text-orange-600'
            }`}>
              <Icon size={18} />
            </div>
            <p className="text-2xl font-bold text-gray-900">{value}</p>
            <p className="text-sm text-gray-500 mt-0.5">{label}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Recent Applications */}
        <div className="lg:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm">
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
            <h2 className="font-semibold text-gray-900">Recent Applications</h2>
            <button
              onClick={() => navigate('/applications')}
              className="text-sm text-blue-600 hover:text-blue-700 font-medium"
            >
              View all →
            </button>
          </div>
          <div className="divide-y divide-gray-50">
            {recent.length === 0 ? (
              <p className="px-6 py-8 text-center text-gray-400 text-sm">No applications yet.</p>
            ) : (
              recent.map((app) => (
                <div
                  key={app.id}
                  onClick={() => navigate(`/applications/${app.id}`)}
                  className="flex items-center justify-between px-6 py-3.5 hover:bg-gray-50 cursor-pointer transition-colors"
                >
                  <div>
                    <p className="text-sm font-mono font-medium text-gray-900">{app.id.slice(0, 8)}…</p>
                    <p className="text-xs text-gray-400 mt-0.5">{new Date(app.createdAt).toLocaleString()}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-xs text-gray-400">{app.documents.length} doc{app.documents.length !== 1 ? 's' : ''}</span>
                    <AppStatusBadge status={app.status} />
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Quick Actions */}
        <div className="lg:col-span-1 flex flex-col gap-4">
          {/* New Loan Booking */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center gap-3 mb-3">
              <div className="p-2 bg-emerald-100 text-emerald-600 rounded-lg">
                <CreditCard size={18} />
              </div>
              <h2 className="font-semibold text-gray-900">Loan Booking</h2>
            </div>
            <p className="text-sm text-gray-500 mb-4">
              Submit a new loan application for credit assessment and approval.
            </p>
            <button
              onClick={() => navigate('/loans/new')}
              className="w-full flex items-center justify-center gap-2 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-sm font-medium rounded-lg transition-colors"
            >
              <Plus size={15} />
              New Loan Application
            </button>
          </div>

          {/* New Account Opening */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <div className="flex items-center gap-3 mb-3">
              <div className="p-2 bg-blue-100 text-blue-600 rounded-lg">
                <FileText size={18} />
              </div>
              <h2 className="font-semibold text-gray-900">Account Opening</h2>
            </div>
            <p className="text-sm text-gray-500 mb-4">
              Start a new customer account opening application with document upload.
            </p>
            <button
              onClick={() => navigate('/applications/new')}
              className="w-full flex items-center justify-center gap-2 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
            >
              <Plus size={15} />
              New Application
            </button>
          </div>
        </div>

        {/* AI Agent Quickstart */}
        <div className="bg-gradient-to-br from-blue-600 to-blue-700 rounded-xl text-white p-6 flex flex-col">
          <div className="p-3 bg-white/20 rounded-xl w-fit mb-4">
            <Bot size={24} />
          </div>
          <h2 className="font-semibold text-lg mb-2">AI Banking Agent</h2>
          <p className="text-blue-100 text-sm mb-6 flex-1">
            Use the AI agent to review applications, run BVN verification, fraud assessments,
            and process accounts end-to-end in seconds.
          </p>
          <button
            onClick={() => navigate('/agent')}
            className="w-full py-2.5 bg-white text-blue-700 font-medium rounded-lg text-sm hover:bg-blue-50 transition-colors"
          >
            Open AI Agent →
          </button>
        </div>
      </div>
    </div>
  );
}
