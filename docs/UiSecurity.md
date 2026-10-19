# UI and sign-in changes

Based on the professor's notes in Stuff.docx. This is a partial implementation.

- Settings shows the signed-in staff name, username, barangay profile, and logout.
- Login masks passwords, clears submitted password fields, and uses general errors.
- Ctrl+Shift+R no longer opens signup. Only an empty database offers first-account setup.
- The existing account-creation procedure now checks that no users exist while holding a database lock. Concurrent setup attempts cannot create two first accounts.
- New setup passwords need 15–128 characters. New hashes use PBKDF2-SHA256 with 600,000 iterations; existing 100,000-iteration hashes still work.
- Five failed sign-ins pause this login window for 30 seconds. This is a local UI delay, not an account lockout; restarting the app bypasses it.
- The footer is removed. Database errors and connection details are not displayed on the login or record screens.
- The main window supports 800×560. Action buttons wrap, search fields have more space, and grids scroll horizontally when columns cannot fit.
- The Requests button says New Request. Its existing confirmation stays in place; the fee basis is kept in storage but removed from the request form.

First setup must be completed by authorized staff. After that, account provisioning and password recovery need the team's account-management implementation; this UI does not provide public registration or invent administrator privileges.

## Still for the logic/database team

Role checks on operations, persistent failed-attempt controls, audit logs, password changes/recovery, and idle-session locking are not implemented by this UI pass. The client still connects directly to MySQL. Database credentials, database privileges, and transport security also need review before using real resident data.

The rest of the professor's list—including households/dependents, census, office-hour workflow, Crystal Reports, and fee-policy verification—remains separate work. No claim of complete security or complete compliance is made.

References: [OWASP authentication guidance](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html) and [password storage guidance](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html).
