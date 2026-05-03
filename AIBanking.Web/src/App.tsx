import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Applications from './pages/Applications';
import ApplicationDetail from './pages/ApplicationDetail';
import Workflows from './pages/Workflows';
import AgentChat from './pages/AgentChat';
import Users from './pages/Users';
import Forms from './pages/Forms';
import WorkflowReview from './pages/WorkflowReview';
import AccountOpeningForm from './pages/AccountOpeningForm';
import LoanBookingForm from './pages/LoanBookingForm';

function RequireAuth({ children }: { children: React.ReactNode }) {
  const token = localStorage.getItem('token');
  if (!token) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route
          path="/*"
          element={
            <RequireAuth>
              <Layout>
                <Routes>
                  <Route path="/"                      element={<Dashboard />} />
                  <Route path="/applications"           element={<Applications />} />
                  <Route path="/applications/new"       element={<AccountOpeningForm />} />
                  <Route path="/applications/:id"       element={<ApplicationDetail />} />
                  <Route path="/workflows"              element={<Workflows />} />
                  <Route path="/workflows/:id"          element={<WorkflowReview />} />
                  <Route path="/agent"                  element={<AgentChat />} />
                  <Route path="/users"                  element={<Users />} />
                  <Route path="/forms"                  element={<Forms />} />
                  <Route path="/loans/new"              element={<LoanBookingForm />} />
                </Routes>
              </Layout>
            </RequireAuth>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}
