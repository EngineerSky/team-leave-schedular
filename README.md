# Team Leave Scheduler

A lightweight internal HR tool that lets a manager view a 30-day team leave calendar, submit leave requests, and approve or reject pending requests.

Built with **ASP.NET Core 9** (Clean Architecture) · **React + TypeScript** (Vite) · **SQLite** (EF Core)

---

## Prerequisites

| Tool | Version | Check |
|------|---------|-------|
| .NET SDK | 9.0+ | `dotnet --version` |
| Node.js | 18+ | `node --version` |
| npm | 9+ | `npm --version` |
| Git | any | `git --version` |

---

## Quick Start

### 1. Clone the repository

```bash
git clone https://github.com/YOUR-USERNAME/team-leave-scheduler.git
cd team-leave-scheduler
```

### 2. Run the backend (ASP.NET Core API)

Open a terminal in the project root:

```bash
cd LeaveScheduler.Api
dotnet run
```

The API starts on **http://localhost:5000**  
The SQLite database (`leavescheduler.db`) is created and seeded automatically on first run with:
- 3 teams: **Engineering**, **Operations**, **Finance**
- 15 employees (5 per team) with leave balances
- 11 public holidays for 2026

> **Note:** Keep this terminal open while using the app.

### 3. Run the frontend (React + Vite)

Open a **second** terminal in the project root:

```bash
cd frontend
npm install
npm run dev
```

Open your browser at **http://localhost:3000**

---

## Running the Tests

From the project root:

```bash
dotnet test
```

Expected output: **23 passed, 0 failed**

---

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/teams` | List all teams |
| `GET` | `/api/teams/{id}/employees` | List employees in a team |
| `GET` | `/api/employees` | List all employees with balances |
| `GET` | `/api/leaverequests/calendar?teamId=1&startDate=2026-07-01` | 30-day leave calendar for a team |
| `GET` | `/api/leaverequests?teamId=1&status=Pending` | List leave requests (filter optional) |
| `POST` | `/api/leaverequests` | Submit a new leave request |
| `POST` | `/api/leaverequests/{id}/approve` | Approve a pending request |
| `POST` | `/api/leaverequests/{id}/reject` | Reject a pending request |

### Example: Submit a leave request

```bash
curl -X POST http://localhost:5000/api/leaverequests \
  -H "Content-Type: application/json" \
  -d '{"employeeId": 1, "startDate": "2026-07-06", "endDate": "2026-07-10"}'
```

### Example: Approve request #1

```bash
curl -X POST http://localhost:5000/api/leaverequests/1/approve
```

---

## Business Rules

1. **30% Capacity Rule** — At most `max(1, floor(0.30 × teamSize))` employees may be on approved leave on any single working day.
2. **Checked at approval time** — The 30% check runs when a manager approves, not when the employee submits.
3. **Multi-day requests** — If any single working day in the range fails the 30% check, the entire request is rejected.
4. **Leave balance deduction** — Only working days are deducted (weekends and public holidays are excluded).
5. **Overlapping requests** — Two pending requests for the same employee can coexist; approving one automatically rejects any others that overlap.

See [PROPOSAL.md](./PROPOSAL.md) for the full rationale behind each interpretation.

---

## Project Structure

```
team-leave-scheduler/
├── LeaveScheduler.Domain/          # Entities, enums, LeaveRules (business logic)
├── LeaveScheduler.Application/     # Service layer, IApplicationDbContext interface
├── LeaveScheduler.Infrastructure/  # EF Core + SQLite, DbInitializer (seed data)
├── LeaveScheduler.Api/             # ASP.NET Core REST controllers
├── LeaveScheduler.Tests/           # xUnit tests (23 tests)
├── frontend/                       # React + TypeScript (Vite)
│   └── src/
│       ├── App.tsx                 # Calendar, submit form, approval panel
│       ├── api.ts                  # API client
│       └── types.ts                # TypeScript interfaces
├── seed/
│   ├── employees.csv               # Reference seed data (15 employees)
│   └── public_holidays.json        # Reference seed data (2026 holidays)
├── DECISIONS.md                    # 3 design choices with alternatives
├── AI_USAGE.md                     # AI tools used, prompts, and corrections
└── PROPOSAL.md                     # Business rules interpretation
```

---

## Design Decisions

See [DECISIONS.md](./DECISIONS.md) for the full rationale. Summary:

1. **`max(1, floor(0.30 × teamSize))`** — floor strictly honours the 30% cap; max(1,…) prevents impossibility for teams of 1–3.
2. **Approval-time capacity check** — keeps submission frictionless; enforcement is the manager's decision with full context.
3. **Plain service class over MediatR** — correct abstraction for 4 operations; MediatR would be premature at this scope.
