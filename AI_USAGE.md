# AI_USAGE.md — Team Leave Scheduler

## Tools Used
- **Antigravity (Google DeepMind)** — primary coding assistant used throughout the project for code generation, incremental commits, bug fixes, and test writing.

---

## Two Most Useful Prompts

### 1. Clarifying the 30% rule interpretation
> *"30% Rule Formula: Allowed Leave Limit = max(1, floor(0.30 × Team Size)). Plain floor is used because it strictly honors the literal 30% cap for every team size where 30% is achievable with whole people; the max(1, ...) floor activates only for teams of 1–3, where strict compliance would make leave structurally impossible, which we judged to be an unintended consequence of the rule rather than its intent."*

**Why useful:** Rather than asking the AI to guess the business rule interpretation, providing the fully-reasoned interpretation upfront produced a precise `CalculateAllowedLimit` implementation and matching test cases on the first attempt, covering all boundary conditions from team size 1 through 10.

### 2. Specifying the test strategy for the service layer
> *"Write unit tests for overlapping request auto-rejections — include a non-overlapping pending request that must remain Pending after the approval, to prove the system only rejects overlapping ones."*

**Why useful:** This specificity forced the AI to produce a three-request test scenario (approve one, auto-reject the overlapping one, keep the non-overlapping one Pending), which is a much stronger assertion than simply checking that one request was rejected. It caught a potential bug where a naive implementation could have rejected all pending requests instead of just the overlapping ones.

---

## Where AI Led Me Wrong — and How I Corrected It

**What happened:** When generating the initial unit tests for `LeaveApplicationServiceTests`, the AI created two tests (`SubmitLeaveRequest_ShouldSucceed_WhenNoOverlappingApproved` and `SubmitLeaveRequest_ShouldThrow_WhenOverlappingApprovedExists`) that added an `Employee` with a hardcoded `TeamId = 1` — without first creating a `Team` with that ID. 

When the tests ran against an in-memory SQLite database with foreign key enforcement enabled, both tests failed with:
```
SQLite Error 19: 'FOREIGN KEY constraint failed'.
```

**How I corrected it:** I identified the pattern: every test that creates an `Employee` must first insert a `Team` and use the generated `team.Id` as the foreign key, rather than hardcoding `1`. I applied this fix to both failing tests, re-ran `dotnet test`, and all 23 tests passed.

**Lesson:** AI-generated test code often skips relational setup when writing isolated unit tests, because it optimises for brevity. When testing against a real (even in-memory) database with FK constraints, the full entity graph must be seeded in the correct order.
