import { useEffect, useState, useCallback } from 'react';
import type { Team, Employee, TeamCalendar, LeaveRequest } from './types';
import * as api from './api';

// ─── Helpers ───────────────────────────────────────────────────────────────
const fmt = (iso: string) => iso.slice(0, 10);
const today = () => new Date().toISOString().slice(0, 10);

// ─── Calendar Section ───────────────────────────────────────────────────────
function CalendarView({ teamId }: { teamId: number }) {
  const [cal, setCal] = useState<TeamCalendar | null>(null);
  const [error, setError] = useState('');

  const load = useCallback(() => {
    api.getCalendar(teamId, today())
      .then(setCal)
      .catch(() => setError('Failed to load calendar'));
  }, [teamId]);

  useEffect(() => { load(); }, [load]);

  if (error) return <p style={{ color: 'red' }}>{error}</p>;
  if (!cal) return <p>Loading calendar…</p>;

  return (
    <section>
      <h2>30-Day Leave Calendar — {cal.teamName}</h2>
      <p>Team size: {cal.teamSize} | Allowed concurrently on leave: <strong>{cal.allowedLimit}</strong></p>
      <div style={{ overflowX: 'auto' }}>
        <table border={1} cellPadding={4} cellSpacing={0} style={{ borderCollapse: 'collapse', fontSize: 13 }}>
          <thead>
            <tr>
              <th>Date</th>
              <th>Day</th>
              <th>Working?</th>
              <th>On Leave</th>
              <th>Capacity</th>
            </tr>
          </thead>
          <tbody>
            {cal.days.map(d => {
              const count = d.employeesOnLeave.length;
              const atLimit = d.isWorkingDay && count >= d.allowedLimit;
              return (
                <tr key={d.date} style={{ background: atLimit ? '#ffe0e0' : d.isWorkingDay ? '#fff' : '#f5f5f5' }}>
                  <td>{fmt(d.date)}</td>
                  <td>{new Date(d.date).toLocaleDateString('en-GB', { weekday: 'short' })}</td>
                  <td style={{ textAlign: 'center' }}>{d.isWorkingDay ? '✓' : '–'}</td>
                  <td>{d.employeesOnLeave.map(e => e.employeeName).join(', ') || '—'}</td>
                  <td style={{ textAlign: 'center' }}>
                    {d.isWorkingDay ? `${count} / ${d.allowedLimit}` : '—'}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}

// ─── Submit Request Form ─────────────────────────────────────────────────────
function SubmitForm({ employees, onSubmitted }: { employees: Employee[]; onSubmitted: () => void }) {
  const [employeeId, setEmployeeId] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [msg, setMsg] = useState('');
  const [error, setError] = useState('');

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setMsg(''); setError('');
    try {
      await api.submitLeaveRequest(Number(employeeId), startDate, endDate);
      setMsg('Request submitted successfully.');
      setEmployeeId(''); setStartDate(''); setEndDate('');
      onSubmitted();
    } catch (err) {
      setError(String(err));
    }
  };

  return (
    <section>
      <h2>Submit Leave Request</h2>
      <form onSubmit={submit} style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'flex-end' }}>
        <div>
          <label>Employee<br />
            <select required value={employeeId} onChange={e => setEmployeeId(e.target.value)}>
              <option value="">— select —</option>
              {employees.map(emp => (
                <option key={emp.id} value={emp.id}>
                  {emp.name} ({emp.teamName}, balance: {emp.leaveBalance}d)
                </option>
              ))}
            </select>
          </label>
        </div>
        <div>
          <label>Start Date<br /><input required type="date" value={startDate} onChange={e => setStartDate(e.target.value)} /></label>
        </div>
        <div>
          <label>End Date<br /><input required type="date" value={endDate} onChange={e => setEndDate(e.target.value)} /></label>
        </div>
        <button type="submit">Submit</button>
      </form>
      {msg && <p style={{ color: 'green' }}>{msg}</p>}
      {error && <p style={{ color: 'red' }}>{error}</p>}
    </section>
  );
}

// ─── Pending Requests Panel ──────────────────────────────────────────────────
function PendingRequests({ teamId, key: _key }: { teamId: number; key: number }) {
  const [requests, setRequests] = useState<LeaveRequest[]>([]);
  const [msg, setMsg] = useState('');
  const [error, setError] = useState('');

  const load = useCallback(() => {
    api.getLeaveRequests(teamId).then(setRequests);
  }, [teamId]);

  useEffect(() => { load(); }, [load]);

  const approve = async (id: number) => {
    setMsg(''); setError('');
    try {
      const res = await api.approveRequest(id);
      setMsg(res.message);
      load();
    } catch (err) { setError(String(err)); }
  };

  const reject = async (id: number) => {
    setMsg(''); setError('');
    const reason = prompt('Reason for rejection (optional):') ?? '';
    try {
      const res = await api.rejectRequest(id, reason);
      setMsg(res.message);
      load();
    } catch (err) { setError(String(err)); }
  };

  const pending = requests.filter(r => r.status === 'Pending');
  const others  = requests.filter(r => r.status !== 'Pending');

  return (
    <section>
      <h2>All Leave Requests</h2>
      {msg   && <p style={{ color: 'green' }}>{msg}</p>}
      {error && <p style={{ color: 'red' }}>{error}</p>}

      <h3>Pending ({pending.length})</h3>
      {pending.length === 0
        ? <p>No pending requests.</p>
        : (
          <table border={1} cellPadding={4} cellSpacing={0} style={{ borderCollapse: 'collapse', fontSize: 13 }}>
            <thead>
              <tr><th>ID</th><th>Employee</th><th>Start</th><th>End</th><th>Actions</th></tr>
            </thead>
            <tbody>
              {pending.map(r => (
                <tr key={r.id}>
                  <td>{r.id}</td>
                  <td>{r.employeeName}</td>
                  <td>{fmt(r.startDate)}</td>
                  <td>{fmt(r.endDate)}</td>
                  <td>
                    <button onClick={() => approve(r.id)}>✓ Approve</button>{' '}
                    <button onClick={() => reject(r.id)}>✗ Reject</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )
      }

      <h3 style={{ marginTop: 16 }}>History</h3>
      {others.length === 0
        ? <p>No history yet.</p>
        : (
          <table border={1} cellPadding={4} cellSpacing={0} style={{ borderCollapse: 'collapse', fontSize: 13 }}>
            <thead>
              <tr><th>ID</th><th>Employee</th><th>Start</th><th>End</th><th>Status</th><th>Reason</th></tr>
            </thead>
            <tbody>
              {others.map(r => (
                <tr key={r.id} style={{ background: r.status === 'Approved' ? '#e8f5e9' : '#fce4ec' }}>
                  <td>{r.id}</td>
                  <td>{r.employeeName}</td>
                  <td>{fmt(r.startDate)}</td>
                  <td>{fmt(r.endDate)}</td>
                  <td><strong>{r.status}</strong></td>
                  <td>{r.statusReason ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )
      }
    </section>
  );
}

// ─── Root App ────────────────────────────────────────────────────────────────
export default function App() {
  const [teams, setTeams] = useState<Team[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [teamId, setTeamId] = useState<number>(0);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    api.getTeams().then(ts => {
      setTeams(ts);
      if (ts.length > 0) setTeamId(ts[0].id);
    });
    api.getEmployees().then(setEmployees);
  }, []);

  const teamEmployees = employees.filter(e => e.teamId === teamId);
  const refresh = () => setRefreshKey(k => k + 1);

  return (
    <div style={{ fontFamily: 'sans-serif', maxWidth: 960, margin: '0 auto', padding: 16 }}>
      <h1>Team Leave Scheduler</h1>

      <div style={{ marginBottom: 16 }}>
        <label><strong>Team: </strong>
          <select value={teamId} onChange={e => { setTeamId(Number(e.target.value)); refresh(); }}>
            {teams.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
        </label>
      </div>

      <hr />
      {teamId > 0 && <CalendarView key={`cal-${teamId}-${refreshKey}`} teamId={teamId} />}
      <hr />
      <SubmitForm employees={teamEmployees} onSubmitted={refresh} />
      <hr />
      {teamId > 0 && <PendingRequests key={refreshKey} teamId={teamId} />}
    </div>
  );
}
