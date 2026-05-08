import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Search, RefreshCw } from 'lucide-react';
import { applicationsApi } from '../api/applications';
import type { AccountApplication, AccountStatus } from '../types';
import { AppStatusBadge } from '../components/StatusBadge';

const STATUS_OPTIONS: { label: string; value: AccountStatus | '' }[] = [
  { label: 'All Statuses', value: '' },
  { label: 'Draft',              value: 'Draft' },
  { label: 'Pending Documents',  value: 'PendingDocuments' },
  { label: 'Under Review',       value: 'UnderReview' },
  { label: 'Approved',           value: 'Approved' },
  { label: 'Active',             value: 'Active' },
  { label: 'Rework',             value: 'Rework' },
  { label: 'Rejected',           value: 'Rejected' },
];

export default function Applications() {
  const navigate = useNavigate();
  const [apps,    setApps]    = useState<AccountApplication[]>([]);
  const [filter,  setFilter]  = useState<AccountStatus | ''>('');
  const [search,  setSearch]  = useState('');
  const [loading, setLoading] = useState(true);
  async function load() {
    setLoading(true);
    try {
      const data = await applicationsApi.list(filter || undefined);
      setApps(data);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, [filter]); // eslint-disable-line react-hooks/exhaustive-deps

  const displayed = search
    ? apps.filter(a => a.id.toLowerCase().includes(search.toLowerCase()) ||
                       a.status.toLowerCase().includes(search.toLowerCase()) ||
                       a.extractedInfo?.fullName?.toLowerCase().includes(search.toLowerCase()))
    : apps;

  return (
    <div className="p-8">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Applications</h1>
          <p className="text-gray-500 mt-1">Account opening applications</p>
        </div>
        <button
          onClick={() => navigate('/applications/new')}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
        >
          <Plus size={16} />
          New Application
        </button>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3 mb-5">
        <div className="relative flex-1 max-w-xs">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            placeholder="Search by ID or name…"
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="w-full pl-9 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <select
          value={filter}
          onChange={e => setFilter(e.target.value as AccountStatus | '')}
          className="px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 bg-white"
        >
          {STATUS_OPTIONS.map(o => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
        <button
          onClick={load}
          className="p-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-gray-500 transition-colors"
          title="Refresh"
        >
          <RefreshCw size={16} />
        </button>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="animate-spin w-7 h-7 border-4 border-blue-500 border-t-transparent rounded-full" />
          </div>
        ) : displayed.length === 0 ? (
          <div className="py-20 text-center">
            <FileTextIcon />
            <p className="text-gray-500 mt-3 text-sm">No applications found.</p>
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">ID</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Applicant</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Documents</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">Created</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wide">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {displayed.map(app => (
                <tr
                  key={app.id}
                  onClick={() => navigate(`/applications/${app.id}`)}
                  className="hover:bg-gray-50 cursor-pointer transition-colors"
                >
                  <td className="px-6 py-4 font-mono text-xs text-gray-600">{app.id.slice(0, 13)}…</td>
                  <td className="px-6 py-4 text-gray-900">
                    {app.extractedInfo?.fullName ?? <span className="text-gray-400 italic">Not extracted</span>}
                  </td>
                  <td className="px-6 py-4"><AppStatusBadge status={app.status} /></td>
                  <td className="px-6 py-4 text-gray-500">{app.documents.length} / 2</td>
                  <td className="px-6 py-4 text-gray-500">{new Date(app.createdAt).toLocaleDateString()}</td>
                  <td className="px-6 py-4 text-right">
                    <span className="text-blue-600 hover:text-blue-700 font-medium text-xs">View →</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
      <p className="text-xs text-gray-400 mt-3">{displayed.length} result{displayed.length !== 1 ? 's' : ''}</p>
    </div>
  );
}

function FileTextIcon() {
  return (
    <svg className="w-12 h-12 mx-auto text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
        d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
    </svg>
  );
}
