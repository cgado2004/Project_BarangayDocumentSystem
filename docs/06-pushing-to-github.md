# Pushing this to GitHub

*Written by Clint Wood Gado.*

Repository: `https://github.com/cgado2004/Project_BarangayDocumentSystem`
Branch: **`leader_draft`**

---

## 1. Why I could not push it myself

I build this in a sandbox with no GitHub credentials. Fetching works — it is
public, and I used that to read what was already on `leader_draft` — but
pushing needs a personal access token, and I am not willing to have anyone
paste a token into a chat log where it would be stored. So I prepared the
commit and left the one authenticated step to be run on my own machine.

---

## 2. What I found on the branch first

I did **not** just push over it. Fetching first showed `leader_draft` already
had **9 commits** of the group's work, on the old v2 three-project structure:

```
93d89ec  Update README to remove submission checklist
350aec7  Fix four string literals broken by raw newlines
d7f0e72  Grant Infrastructure access to Domain internals
dc4e7ea  List UI project first in the solution
8edd49c  Fix peso sign regression
6c0bf1e  Add group members
37cbe48  Update documentation for the three-project structure
a4bca39  Refactor into Domain/Infrastructure/UI layers
ec27edf  Add Barangay Resident and Document Request Management System
```

My v3 folder was started fresh, so it had **no shared history** with that
branch at all. Pushing it directly would have needed `--force`, and
`--force` on a shared branch **deletes everybody else's commits.** Those nine
commits are the group's work and are not mine to erase.

---

## 3. What I did instead

I rebuilt v3 as a single commit **on top of** the existing history, rather
than as a replacement for it:

```
74bc341  Version 3: two-layer restructure, Form1 + designer, real data   <- mine
93d89ec  Update README to remove submission checklist                    <- the group's
350aec7  ...                                                                 (all 9 kept)
```

The files end up exactly the same as my v3 folder — I verified the two trees
are byte-for-byte identical — but the earlier commits are still there. Anyone
can still run `git log` and see what the group did, and `git revert` my commit
if they want v2 back.

Because my commit sits on top, the push is a plain **fast-forward**. No
`--force`, nothing overwritten.

---

## 4. How to push it

From the `BarangayDocumentSystemV3` folder:

```bash
bash push.sh
```

The script fetches first and refuses to push if somebody else has committed in
the meantime, rather than blindly overwriting them.

Or by hand:

```bash
git fetch origin leader_draft
git merge-base --is-ancestor origin/leader_draft v3-on-leader   # must succeed
git push origin v3-on-leader:leader_draft
```

Git will ask for a username and password. The password is **not** the account
password — GitHub stopped accepting those in 2021. Use a **personal access
token**: GitHub → Settings → Developer settings → Personal access tokens →
Tokens (classic) → Generate new token, tick **repo**, copy it, and paste it as
the password.

---

## 5. Fetching and pulling later

```bash
git fetch origin                       # see what changed, change nothing locally
git pull --rebase origin leader_draft  # bring their commits in under mine
```

I use `--rebase` rather than a plain `pull` so the history stays a straight
line instead of collecting merge commits every time two of us work on the
same day.

---

## 6. If the push is rejected

`! [rejected] ... (non-fast-forward)` means a group-mate pushed while I was
working. This is normal and nothing is lost:

```bash
git pull --rebase origin leader_draft
# fix any conflicts git reports, then:
git push origin v3-on-leader:leader_draft
```

**Do not use `git push --force` to make the error go away.** That is exactly
the command that deletes a team-mate's work, and the error is Git protecting
them.

---

## 7. Branches in my local folder

| Branch | What it is |
|---|---|
| `v3-on-leader` | **Push this one.** V3 stacked on the group's history. |
| `v3-draft` | V3 on its own, with no shared history. Kept only as a backup. |

---

## 8. One thing to tell the group

This commit **changes the project structure** — `Domain`, `Infrastructure` and
`UI` become `Core` and `App`. Anyone with local work in progress should commit
or stash it before pulling, or they will be resolving conflicts against files
that no longer exist.

It is worth a message in the group chat before the push, not after.
