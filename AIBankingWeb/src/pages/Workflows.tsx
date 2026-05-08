import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { RefreshCw, ChevronRight } from 'lucide-react';
import { workflowsApi } from '../api/workflows';
import type { WorkflowItem, WorkflowStatus } from '../types';
import { WorkflowStatusBadge } from '../components/StatusBadge';

const STATUS_FILTERS: { label: string; value: WorkflowStatus | '' }[] = [
  { label: 'All',      value: '' },
  { label: 'Pending',  value: 'Pending' },
  { label: 'Rework',   value: 'Rework' },
  { label: 'Approved', value: 'Approved' },
  { label: 'Declined', value: 'Declined' },
];

export default function Workflows() {
  const navigate = useNavigate();
  const [items,   setItems]   = useState<WorkflowItem[]>([]);
  const [filter,  setFilter]  = useState<WorkflowStatus | ''>('');
  const [loading, setLoading] = useState(true);

  async function load() {
    setLoading(true);
    try {
      setItems(await workflowsApi.list(filter || undefined));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, [filter]); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Workflows</h1>
          <p className="text-sm text-gray-500 mt-0.5">Click a row to review and act</p>
        </div>
        <button onClick={load} className="p-2 border border-gray-300 rounded-lg hover:bg-gray-50 text-gray-500">
          <RefreshCw size={16} />
        </button>
      </div>

      {/* Status filter tabs */}
      <div className="flex gap-1 bg-gray-100 p-1 rounded-lg w-fit mb-5">
        {STATUS_FILTERS.map(f => (
          <button
            key={f.value}
            onClick={() => setFilter(f.value)}
            className={`px-3 py-1.5 text-sm rounded-md transition-colors ${
              filter === f.value
                ? 'bg-white text-gray-900 shadow-sm font-medium'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            {f.label}
          </button>
        ))}
      </div>

      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="animate-spin w-7 h-7 border-4 border-blue-500 border-t-transparent rounded-full" />
          </div>
        ) : items.length === 0 ? (
          <p className="py-20 text-center text-gray-400 text-sm">No workflows found.</p>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase">Title</th>
                <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase">Submitted by</th>
                <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase">Current stage</th>
                <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                <th className="px-5 py-3 text-left text-xs font-medium text-gray-500 uppercase">Created</th>
                <th className="px-5 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {items.map(item => {
                const currentStage = item.definition?.stages.find(
                  s => s.stageOrder === item.currentStageOrder
                );
                return (
                  <tr
                    key={item.id}
                    onClick={() => navigate(`/workflows/${item.id}`)}
                    className="hover:bg-blue-50 cursor-pointer transition-colors"
                  >
                    <td className="px-5 py-4">
                      <p className="font-medium text-gray-900">{item.title}</p>
                      {item.definition && (
                        <p className="text-xs text-gray-400 mt-0.5">{item.definition.name}</p>
                      )}
                    </td>
                    <td className="px-5 py-4 text-gray-600">{item.submittedBy}</td>
                    <td className="px-5 py-4">
                      {currentStage && item.status === 'Pending' ? (
                        <span className="inline-flex px-2 py-0.5 rounded-full bg-blue-50 text-blue-700 text-xs font-medium">
                          {currentStage.stageName}
                        </span>
                      ) : (
                        <span className="text-gray-400 text-xs">—</span>
                      )}
                    </td>
                    <td className="px-5 py-4"><WorkflowStatusBadge status={item.status} /></td>
                    <td className="px-5 py-4 text-gray-500 text-xs">
                      {new Date(item.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-5 py-4 text-right">
                      <ChevronRight size={16} className="text-gray-400 inline" />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
