# Walkthrough and manual checks

Keep `LoadSampleData=true` in `App.config` for this walkthrough.

## First run

Open the solution and press F5. The dashboard should show:

- 7 residents and 6 requests.
- 2 pending, 1 processing, 1 ready, 2 released, 0 rejected.
- PHP 50.00 collected.
- 1 free document released.

The sample people and receipts are fictional.

## Residents

1. Open Residents and search for Maria, then clear the search.
2. Register a resident with required names, address, purok, birth date,
   residency date, and a contact number containing 7–15 digits.
3. Edit that resident's address, save, and search for the updated record.
4. Try an empty first name, a pasted alphabetic contact number, a future birth
   date, and a residency date before birth. Each should show a message without saving.
5. Delete the new resident before creating any requests. It should succeed.
6. Try deleting Juan. It should be blocked because he has request history.

## Request through release

1. Select Juan and choose File request.
2. Choose Barangay Clearance, enter a purpose, and confirm the PHP 50.00 fee.
3. File it, then open Document Requests and select the new reference.
4. Choose Start processing, then Mark ready.
5. Release should stay disabled while the request is unpaid.
6. Choose Record payment and enter a new official receipt number.
7. Choose Release and confirm.
8. Open View / print. The text should no longer have a draft heading.
9. Open Print preview, then use Print to choose Microsoft Print to PDF or a
   configured printer. Cancel the print dialog to check that cancellation is harmless.

Preview a Pending request too: its text is visible, but printing is disabled.

## Fees and eligibility

- Select Juan and choose Certificate of Residency, then Certificate of Good
  Moral Character. Each should show PHP 100.00 with a Citizen's Charter basis.
- Maria's personal certificates are free under the classroom senior policy.
- Maria's business clearance still costs PHP 200.00 and requires business details.
- Liza's personal certificates are free under the classroom PWD policy.
- Certificate of Indigency is free.
- Carlo cannot request a first-time jobseeker certificate with only two months
  of residency.
- Jose already has a Processing first-time jobseeker request. Release that
  existing request, then confirm another one is blocked.

## History and rejection

- Record a payment, then reject the request with a reason. The receipt and
  collection total must remain recorded.
- Try reusing its receipt number on another request; this should be blocked.
- Edit a resident with a Released document, then open that document again.
  Its old name and address should remain.
- A rejected request should not allow processing, payment, or release.

## Session behavior

Close the app and confirm the warning. Reopen it: the sample records return,
but records you added in the previous session do not. With
`LoadSampleData=false`, it starts empty instead.

## Automated checks

Build and run `Tests/BarangayDocumentSystem.Tests.csproj` using the commands
in the README. The checks exercise validation, workflows, fees, receipts,
historical text, templates, form submissions, and multi-page printing.

The printer check renders using the Microsoft Print to PDF driver and a
preview controller. It does not submit a physical print job.
