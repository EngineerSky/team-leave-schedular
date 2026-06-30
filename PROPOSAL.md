# Business Rules Proposal - Team Leave Scheduler

This proposal contains the chosen interpretations of the business rules governing the Team Leave Scheduler application. Please review these rules before approving the implementation plan.

---

## 1. 30% Rule Formula
* **Interpretation:**
  $$\text{Allowed Leave Limit} = \max(1, \lfloor 0.30 \times \text{Team Size} \rfloor)$$
* **Details:**
  - Plain floor ($\lfloor \dots \rfloor$) strictly honors the literal $30\%$ limit.
  - The $\max(1, \dots)$ override activates only for teams of 1–3, where strict compliance would make leave structurally impossible (Allowed limit of 0). This is judged to be an unintended consequence of the rule rather than its intent.

## 2. 30% Check Timing
* **Interpretation:** The $30\%$ check is evaluated at **approval time**, not at submission.
* **Details:**
  - A request may be submitted and remain pending even if it would currently violate the cap.
  - The request is only rejected if approving it would actually violate the cap at the moment of approval.

## 3. Multi-day Leave Request Capacity
* **Interpretation:** Capacity is checked **per working day** at approval time.
* **Details:**
  - If any single day in the requested range fails the $30\%$ check, the entire request is rejected.
  - Non-working days (weekends and public holidays) are excluded from capacity checks.

## 4. Leave Balance Deduction
* **Interpretation:** On approval, the employee's leave balance is decremented only by the **number of working days** in the request.
* **Details:**
  - Weekends and public holidays inside the requested range are excluded from the deduction.
  - The same "working day" definition is used as the $30\%$ check.

## 5. Overlapping Requests
* **Interpretation:**
  - An approved request blocks submission/approval of any overlapping request for the same employee.
  - Two pending requests for the same employee can coexist.
  - The moment one of these pending requests is approved, the other is automatically rejected by the system.
