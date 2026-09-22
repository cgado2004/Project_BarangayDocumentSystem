# Fee and document notes

This is a classroom project. The Citizen's Charter update is in progress:
`FeeSchedule` now uses the photographed board's PHP 100 base fee for residency
and good moral certificates. Other rates and personal exemptions still use
the project's demonstration policy.

| Document | Current base fee | Basis / update status |
|---|---:|---|
| Barangay Clearance | PHP 50 | Classroom placeholder; purpose-based Charter fees pending |
| Certificate of Residency | PHP 100 | Supplied Citizen's Charter photo; updated |
| Certificate of Indigency | PHP 0 | Existing free policy; also shown as free on the board |
| Barangay Business Clearance | PHP 200 | Classroom placeholder; tariff needs clarification |
| Barangay ID | PHP 100 | Classroom placeholder; no price established by the photo |
| First-Time Jobseeker Certificate | PHP 0 | Existing jobseeker policy; see references below |
| Certificate of Good Moral Character | PHP 100 | Supplied Citizen's Charter photo; updated |

The remaining classroom amounts were retained from the
[original project's fee schedule](https://github.com/cgado2004/Project_BarangayDocumentSystem/blob/93d89ecfb49a6ec3399291ffcd88f1321c414b6a/src/BarangayDocumentSystem.Domain/Services/FeeSchedule.cs).
That version explicitly labels its amounts as placeholders. It is the source
of those sample values, not an official price reference.

## Supplied Charter evidence and remaining work

The source is the Barangay Magugpo Poblacion Citizen's Charter photo in
`brgy stuff.docx`: the 12th photo in document order (`word/media/image6.jpg`).
The source document and photos stay outside Git. The photo establishes what
the board displays; its effective date and complete exceptions are unconfirmed.

- Residency and good moral certification base fees are now PHP 100. The form
  displays the Charter basis and saves it with the request.
- Barangay clearance lists PHP 100 for local employment and PHP 200 for
  employment abroad. An explicit purpose choice and fee update are pending;
  other processing fees refer to the barangay tariff.
- Clearance lists one valid ID, purok clearance, and a residence certificate
  (cedula). Certification lists one valid ID and purok clearance. These
  supporting-document requirements are not yet collected or checked by the app.
- Indigency and low-income certificates are listed as free. Low-income and
  other-purpose certification do not have separate document types in the app.
- The business-clearance fee wording is ambiguous and needs barangay
  clarification. The photo establishes no barangay ID price or blanket
  senior/PWD exemption.

Personal exemptions apply to indigent, senior, and PWD classifications in the
existing classroom policy, including residency and good moral requests. This
small base-fee update preserves that behavior; reviewing those exemptions is
still pending. Business clearance keeps its base fee. Student and solo-parent
tags alone do not waive fees. The sample collection total remains PHP 50:
its paid request is a clearance, and its two affected certificates remain
free under the classroom senior/PWD policies.

## References and a documentation correction

[RA 11261, sections 3-5](https://lawphil.net/statutes/repacts/ra2019/ra_11261_2019.html)
and its implementation guidance support the first-time jobseeker
certification feature. The app checks residency and prior use, and includes
a classroom oath. Citizenship is assumed, matching the current scope.
Official forms and guidance are available from
[DOLE's First Time Jobseekers Assistance page](https://ble.dole.gov.ph/ftjaa/) and
[DOLE's implementation notice](https://dole.gov.ph/dole-issues-rules-on-free-documentary-requirements-for-first-time-jobseekers/).

Senior and PWD exemptions are labeled as project policy, with RA 9994 and
RA 10754 as references. The program does not claim these references alone
establish every local barangay fee waiver. See
[RA 9994](https://lawphil.net/statutes/repacts/ra2010/ra_9994_2010.html) and
[NCDA's RA 10754 implementation rules](https://ncda.gov.ph/disability-laws/implementing-rules-and-regulations-irr/irr-of-ra-10754-an-act-expanding-the-benefits-and-privileges-of-persons-with-disability-pwd/).

The earlier documentation links free indigency certificates to DILG MC
2019-177. That attribution should be corrected rather than copied into the app.
Government assessment criteria identify residency and indigency certificates
as outside that circular's covered clearance process; see the
[Pasig City government document, criterion 5.3](https://assets.pasigcity.gov.ph/storage/executive_order/2024/06/11/6695b223ac5c81721086499%7B06-11-2024%7D%20EXECUTIVE%20ORDER%20NO.%2023%20s.%202024%20.pdf).
The app therefore labels the free indigency certificate as project policy.

Before any real deployment, confirm the barangay's approved fee schedule,
exemption evidence, signing official, and document wording with the barangay.
The classroom app does not check case records, verify declarations against
external systems, or process refunds.
