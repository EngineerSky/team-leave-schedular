import type { Team, Employee, TeamCalendar, LeaveRequest } from './types';

const BASE = '/api';

const json = (res: Response) => {
  if (!res.ok) return res.text().then(t => Promise.reject(t));
  return res.json();
};

export const getTeams = (): Promise<Team[]> =>
  fetch(`${BASE}/teams`).then(json);

export const getEmployees = (): Promise<Employee[]> =>
  fetch(`${BASE}/employees`).then(json);

export const getCalendar = (teamId: number, startDate: string): Promise<TeamCalendar> =>
  fetch(`${BASE}/leaverequests/calendar?teamId=${teamId}&startDate=${startDate}`).then(json);

export const getLeaveRequests = (teamId: number, status?: string): Promise<LeaveRequest[]> => {
  const params = new URLSearchParams({ teamId: String(teamId) });
  if (status) params.set('status', status);
  return fetch(`${BASE}/leaverequests?${params}`).then(json);
};

export const submitLeaveRequest = (
  employeeId: number,
  startDate: string,
  endDate: string
): Promise<LeaveRequest> =>
  fetch(`${BASE}/leaverequests`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ employeeId, startDate, endDate }),
  }).then(json);

export const approveRequest = (id: number): Promise<{ message: string }> =>
  fetch(`${BASE}/leaverequests/${id}/approve`, { method: 'POST' }).then(json);

export const rejectRequest = (id: number, reason: string): Promise<{ message: string }> =>
  fetch(`${BASE}/leaverequests/${id}/reject`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ reason }),
  }).then(json);
