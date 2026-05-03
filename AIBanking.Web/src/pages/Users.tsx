import { useEffect, useState, type FormEvent } from 'react';
import { UserPlus, Power, PowerOff, X } from 'lucide-react';
import { getAxiosErrorMessage } from '../utils/errorHandling';

const ROLE_STYLES: Record<string, string> = {
  Admin:          'bg-purple-100 text-purple-700',
  Staff:          'bg-blue-100 text-blue-700',
  Viewer:         'bg-gray-100 text-gray-600',
  Teller:         'bg-cyan-100 text-cyan-700',
  CPC:            'bg-teal-100 text-teal-700',
  TeamLeadCPC:    'bg-teal-100 text-teal-800',
  CreditAnalyst:  'bg-amber-100 text-amber-700',
  TeamLeadCredit: 'bg-amber-100 text-amber-800',
  Compliance:     'bg-rose-100 text-rose-700',
};

const ROLE_LABELS: Record<string, string> = {
  TeamLeadCPC:    'Team Lead CPC',
  TeamLeadCredit: 'Team Lead Credit',
  CreditAnalyst:  'Credit Analyst',
};

function RoleBadge({ role }: { role: string }) {
  return (
    <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${ROLE_STYLES[role] ?? 'bg-gray-100 text-gray-600'}`}>
      {ROLE_LABELS[role] ?? role}
    </span>
  );
}
import { usersApi, type UserRecord, type CreateUserPayload } from '../api/auth';

// ── Create user modal ─────────────────────────────────────────────────────────

interface CreateModalProps {
  onClose: () => void;
  onCreated: (user: UserRecord) => void;
}

function CreateModal({ onClose, onCreated }: CreateModalProps) {
  const [form, setForm] = useState<CreateUserPayload>({
    username: '',
    password: '',
    fullName: '',
    role: 'Staff',
  });
  const [error, setError]     = useState('');
  const [loading, setLoading] = useState(false);

  function set(field: keyof CreateUserPayload, value: string) {
    setForm(f => ({ ...f, [field]: value }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!form.fullName.trim()) { setError('Full name is required.'); return; }
    setError('');
    setLoading(true);
    try {
      const user = await usersApi.create(form);
      onCreated(user);
    } catch (err: unknown) {
      setError(getAxiosErrorMessage(err, 'Failed to create user.'));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md p-6">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-semibold text-gray-900">Create user</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            <X size={20} />
          </button>
        </div>

        {error && (
          <div className="mb-4 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Full name</label>
            <input
              required
              value={form.fullName}
              onChange={e => set('fullName', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Jane Doe"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Username</label>
            <input
              required
              value={form.username}
              onChange={e => set('username', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="janedoe"
              autoComplete="off"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Password</label>
            <input
              required
              type="password"
              value={form.password}
              onChange={e => set('password', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="Min. 8 characters"
              autoComplete="new-password"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Role</label>
            <select
              value={form.role}
              onChange={e => set('role', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <optgroup label="Administration">
                <option value="Admin">Admin</option>
                <option value="Staff">Staff</option>
                <option value="Viewer">Viewer</option>
              </optgroup>
              <optgroup label="Operations">
                <option value="Teller">Teller</option>
                <option value="CPC">CPC</option>
                <option value="TeamLeadCPC">Team Lead CPC</option>
              </optgroup>
              <optgroup label="Credit &amp; Compliance">
                <option value="CreditAnalyst">Credit Analyst</option>
                <option value="TeamLeadCredit">Team Lead Credit</option>
                <option value="Compliance">Compliance</option>
              </optgroup>
            </select>
          </div>

          <div className="flex gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 py-2 border border-gray-300 rounded-lg text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="flex-1 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white font-medium rounded-lg text-sm transition-colors"
            >
              {loading ? 'Creating…' : 'Create user'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function Users() {
  const [users, setUsers]       = useState<UserRecord[]>([]);
  const [loading, setLoading]   = useState(true);
  const [showModal, setModal]   = useState(false);
  const [toggling, setToggling] = useState<string | null>(null);

  const currentRole = localStorage.getItem('role');
  const isAdmin     = currentRole === 'Admin';

  useEffect(() => {
    usersApi.list()
      .then(setUsers)
      .finally(() => setLoading(false));
  }, []);

  function handleCreated(user: UserRecord) {
    setUsers(prev => [...prev, user]);
    setModal(false);
  }

  async function toggleActive(user: UserRecord) {
    setToggling(user.id);
    try {
      await usersApi.setActive(user.id, !user.isActive);
      setUsers(prev =>
        prev.map(u => u.id === user.id ? { ...u, isActive: !u.isActive } : u)
      );
    } finally {
      setToggling(null);
    }
  }

  return (
    <div className="p-6 max-w-5xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Users</h1>
          <p className="text-sm text-gray-500 mt-0.5">{users.length} account{users.length !== 1 ? 's' : ''}</p>
        </div>
        {isAdmin && (
          <button
            onClick={() => setModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
          >
            <UserPlus size={16} />
            New user
          </button>
        )}
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        {loading ? (
          <div className="p-12 text-center text-gray-400 text-sm">Loading…</div>
        ) : users.length === 0 ? (
          <div className="p-12 text-center text-gray-400 text-sm">No users found.</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-gray-600">User</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Username</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Role</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Last login</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Status</th>
                {isAdmin && <th className="px-4 py-3" />}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {users.map(user => (
                <tr key={user.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center font-semibold text-xs shrink-0">
                        {user.fullName[0]?.toUpperCase()}
                      </div>
                      <span className="font-medium text-gray-900">{user.fullName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-gray-500 font-mono">{user.username}</td>
                  <td className="px-4 py-3">
                    <RoleBadge role={user.role} />
                  </td>
                  <td className="px-4 py-3 text-gray-500">
                    {user.lastLoginAt
                      ? new Date(user.lastLoginAt).toLocaleString()
                      : <span className="text-gray-300">Never</span>}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${
                      user.isActive
                        ? 'bg-green-100 text-green-700'
                        : 'bg-red-100 text-red-600'
                    }`}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  {isAdmin && (
                    <td className="px-4 py-3 text-right">
                      <button
                        onClick={() => toggleActive(user)}
                        disabled={toggling === user.id}
                        title={user.isActive ? 'Deactivate' : 'Activate'}
                        className="p-1.5 rounded-lg text-gray-400 hover:text-gray-700 hover:bg-gray-100 disabled:opacity-40 transition-colors"
                      >
                        {user.isActive ? <PowerOff size={15} /> : <Power size={15} />}
                      </button>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showModal && (
        <CreateModal onClose={() => setModal(false)} onCreated={handleCreated} />
      )}
    </div>
  );
}
