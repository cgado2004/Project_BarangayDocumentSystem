# Fee and document notes

This is a classroom project. Rates and exemptions in `FeeSchedule` implement
the project's demonstration policy. They are not verified Magugpo Poblacion
ordinance rates.

| Document | Base classroom fee |
|---|---:|
| Barangay Clearance | PHP 50 |
| Certificate of Residency | PHP 50 |
| Certificate of Indigency | PHP 0 |
| Barangay Business Clearance | PHP 200 |
| Barangay ID | PHP 100 |
| First-Time Jobseeker Certificate | PHP 0 |
| Certificate of Good Moral Character | PHP 50 |

The amounts above were retained from the
[original project's fee schedule](https://github.com/cgado2004/Project_BarangayDocumentSystem/blob/93d89ecfb49a6ec3399291ffcd88f1321c414b6a/src/BarangayDocumentSystem.Domain/Services/FeeSchedule.cs).
That version explicitly labels its amounts as placeholders. It is the source
of the sample values, not an official price reference. No verified Magugpo
Poblacion fee schedule has been supplied for this project; the PHP 50, PHP 100,
and PHP 200 amounts still need confirmation from the barangay's approved
ordinance or published Citizen's Charter.

Personal exemptions apply to indigent, senior, and PWD classifications in the
project policy. Business clearance keeps its base fee. Student and solo-parent
tags alone do not waive fees.

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
