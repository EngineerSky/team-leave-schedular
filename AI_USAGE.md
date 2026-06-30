# AI_USAGE.md — Team Leave Scheduler

## Tools Used
- **Claude (Anthropic)** — used during the planning phase to work through the ambiguous business rule, stress-test candidate formulas against alternatives, and draft/refine the proposal and DECISIONS.md reasoning before any code was written.
- **Antigravity (Google DeepMind)** — primary coding assistant used throughout implementation for code generation, incremental commits, bug fixes, and test writing, executing against the interpretations already locked in during planning.

---

## Two Most Useful Prompts

### 1. Stress-testing the 30% rule formula (Claude, planning phase)
> *"Give me a comparison on both"* — followed by walking through `max(1, floor(0.30 × teamSize))` against `Math.Round` and `ceil` across specific team sizes (2, 5, 7, 15, 25), to see exactly where each alternative diverged and why.

**Why useful:** Rather than picking a rounding strategy on instinct, generating a side-by-side comparison with concrete team sizes surfaced the real failure mode of each alternative — e.g. that `Math.Round`'s banker's-rounding convention produces inconsistent, hard-to-explain results at team sizes near a half-integer (5, 15, 25), which isn't obvious until you tabulate actual numbers. This produced the final formula and its justification before a single line of code was written, so the implementation phase had zero ambiguity left to resolve.

### 2. Specifying the test strategy for the service layer (Antigravity, implementation phase)
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
