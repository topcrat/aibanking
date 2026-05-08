import { NavLink, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  FileText,
  GitBranch,
  Bot,
  LogOut,
  Landmark,
  Users,
  ClipboardList,
} from 'lucide-react';

const navItems = [
  { to: '/',             label: 'Dashboard',    icon: LayoutDashboard, adminOnly: false },
  { to: '/applications', label: 'Applications', icon: FileText,        adminOnly: false },
  { to: '/forms',        label: 'Forms',        icon: ClipboardList,   adminOnly: false },
  { to: '/workflows',    label: 'Workflows',    icon: GitBranch,       adminOnly: false },
  { to: '/agent',        label: 'AI Agent',     icon: Bot,             adminOnly: false },
  { to: '/users',        label: 'Users',        icon: Users,           adminOnly: true  },
];

export default function Layout({ children }: { children: React.ReactNode }) {
  const navigate = useNavigate();
  const username = localStorage.getItem('username') ?? 'Staff';
  const role     = localStorage.getItem('role') ?? '';

  function handleLogout() {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    navigate('/login');
  }

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className="w-64 flex flex-col bg-slate-900 text-white shrink-0">
        {/* Brand */}
        <div className="flex items-center gap-3 px-6 py-5 border-b border-slate-700">
          <div className="p-2 bg-blue-600 rounded-lg">
            <Landmark size={20} />
          </div>
          <div>
            <p className="font-bold text-sm leading-tight">AIBanking</p>
            <p className="text-slate-400 text-xs">Operations Portal</p>
          </div>
        </div>

        {/* Nav */}
        <nav className="flex-1 px-3 py-4 space-y-1">
          {navItems.filter(item => !item.adminOnly || role === 'Admin').map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm transition-colors ${
                  isActive
                    ? 'bg-blue-600 text-white'
                    : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                }`
              }
            >
              <Icon size={18} />
              {label}
            </NavLink>
          ))}
        </nav>

        {/* User */}
        <div className="px-3 py-4 border-t border-slate-700">
          <div className="flex items-center gap-3 px-3 py-2 mb-1">
            <div className="w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center text-sm font-bold shrink-0">
              {username[0]?.toUpperCase()}
            </div>
            <div className="min-w-0">
              <p className="text-sm font-medium truncate">{username}</p>
              <p className="text-slate-400 text-xs">{role || 'Staff'}</p>
            </div>
          </div>
          <button
            onClick={handleLogout}
            className="flex items-center gap-3 w-full px-3 py-2 text-sm text-slate-300 hover:bg-slate-800 hover:text-white rounded-lg transition-colors"
          >
            <LogOut size={16} />
            Sign out
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto">
        {children}
      </main>
    </div>
  );
}
