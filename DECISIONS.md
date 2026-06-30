# DECISIONS.md — Team Leave Scheduler

Three non-trivial design choices made during implementation, with alternatives considered.

---

## 1. 30% Rule: `max(1, floor(0.30 × teamSize))` rather than `ceil` or `round`

**Decision chosen:** Floor with a `max(1,…)` safety net.

**Alternatives considered:**
- **`ceil(0.30 × teamSize)`** — generous; a team of 1 would allow 1, but a team of 6 would allow 2 when the business rule says 30% of 6 is only 1.8, so ceiling would violate the cap (2/6 = 33%).
- **`round(0.30 × teamSize)`** — same problem at the boundary: round(1.5) = 2, which is 40% of 5.
- **Strict floor with no safety net** — correct for every team size ≥ 4, but produces 0 for sizes 1–3, making it structurally impossible to ever approve leave. This is an unintended consequence.

**Why chosen:** `floor` is the only integer operation that never exceeds 30%. The `max(1,…)` guard applies exclusively to the 3 team sizes where floor returns 0, treating those as a rule-drafting oversight rather than intentional design.

---

## 2. 30% check evaluated at **approval time**, not submission time

**Decision chosen:** Capacity is checked at the moment a manager clicks "Approve", not when the employee submits the request.

**Alternatives considered:**
- **Check at submission** — immediately reject submissions that would exceed capacity. Simpler, but means an employee who submits a request during a quiet period could be blocked if a colleague gets approved first, even though their request was submitted earlier. This creates an unpredictable ordering effect.
- **Reserve capacity at submission** — block others from filling the slot once a request is pending. More complex, requires a "soft-hold" concept and risks capacity being held indefinitely by un-actioned pending requests.

**Why chosen:** Approval-time checking keeps submission low-friction (anyone can submit), concentrates the enforcement decision with the manager who has full context, and matches the most natural reading of "the manager decides whether capacity allows it."

---

## 3. `LeaveApplicationService` as a plain class, not MediatR commands/queries

**Decision chosen:** A single `LeaveApplicationService` class with explicit `SubmitLeaveRequestAsync`, `ApproveLeaveRequestAsync`, `RejectLeaveRequestAsync`, and `GetTeamCalendarAsync` methods.

**Alternatives considered:**
- **MediatR with CQRS commands/queries** — industry-standard for larger Clean Architecture solutions; provides loose coupling between API and application logic. However, it adds a non-trivial dependency and abstraction layer (handlers, requests, pipelines) for a project with exactly four operations.
- **In-line controller logic** — no service layer at all; put business rules directly in ASP.NET controllers. Violates Clean Architecture, makes unit testing against a DbContext harder, and conflates HTTP concerns with domain logic.

**Why chosen:** A plain service class provides the testability and layer separation required by Clean Architecture without the overhead of MediatR. At this scope (4 operations, 1 service), the extra indirection of CQRS would be premature abstraction that obscures the business logic rather than clarifying it.
