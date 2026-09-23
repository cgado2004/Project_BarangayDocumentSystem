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
bash scripts/push.sh
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

---

## 9. My Solution Explorer shows folders the README does not have

If you open the folder and see `src/` (with `Domain`, `Infrastructure`,
`UI`), a `Forms/` folder full of `.resx` files, or a capital-`T` `Tests/`
folder — **stop: none of those are in the repository.** The layout in
`README.md` is the whole truth; check it against what git actually tracks:

```powershell
git ls-tree --name-only HEAD      # what the repo really contains
git ls-files | Select-String "^src/|^Forms/|^Tests/"   # -> no matches
```

**Where the ghosts come from.** This group has changed structure more than
once, and git never deletes a file it is not tracking. Three known sources:

| Ghost | Origin |
|---|---|
| `src/Domain` + `src/Infrastructure` + `src/UI` | the OLD three-project layout — still the tip of the orphaned `master` branch. If a copy of that state was ever unpacked or checked out into this folder, it is still sitting there |
| `Forms/*.resx` (PaymentForm, RejectionForm, ResidentForm) | **generated by the Visual Studio designer on your machine**, never committed on any branch — the forms live at the project root, not in a `Forms/` folder |
| `Tests/` (capital T) | Jonathan's Draft branch spelled it `Tests/`; our harness is lowercase `tests/` |

**The cleanup** — look before you leap (`-n` = preview only), then delete:

```powershell
git status --short        # untracked ghosts show as ?? — commit real work FIRST if you see any
git clean -fdn            # DRY RUN: read this list carefully
git clean -fd             # actually remove the untracked folders/files
git pull                  # then sync to the branch you meant to be on
```

`git clean` does not touch tracked files, and it will also remove `bin/`
and `obj/`, which rebuild. If `git status` shows tracked files modified
that you did not touch, ask in the group chat before cleaning.

---

## 10. The .slnx will not open (or Solution Explorer is empty)

**An empty Solution Explorer means NO solution is loaded** - the open
attempt failed. First, know your version (*Help → About Microsoft Visual
Studio*); the answer depends on it:

| Your VS | `.slnx` support |
|---|---|
| **17.14** (GA since the May 2025 release - this includes the September 2026 builds) | **native, no toggle needed** |
| 17.13 | native |
| 17.10-17.12 | preview feature: *Tools → Options → Environment → Preview Features → "Use the XML solution format"*, restart VS |
| older than 17.10 | none - use the `.sln` fallback below |

Then, in order:

1. **Open it the right way.** `.slnx` files do **not** launch Visual Studio
   by double-click the way `.sln` files do. Start Visual Studio, then
   *File → Open → Project/Solution* and pick the `.slnx`. The Solution
   Explorer dropdown should switch to *Solution view* and show two
   projects: `BarangayDocumentSystem` (startup) and `RuleChecks`.
2. **On 17.14 and still empty?** The format is not your problem - the
   workspace is. Run the ten-second test in step 4.
3. **Pane renders blank although a solution should be open?**
   *Window → Reset Window Layout* rebuilds the tool panes.
4. **The ten-second workspace test.** Clone fresh into a NEW folder and
   open the `.slnx` there:

   ```powershell
   git clone https://github.com/cgado2004/Project_BarangayDocumentSystem.git fresh-check
   ```

   Works there? Your old folder was stale or dirty (leftover ghosts from
   §9, an out-of-date checkout). Keep the fresh clone, migrate nothing,
   delete the old folder when you are sure.
5. **No Visual Studio handy, or still failing?** The `dotnet` CLI builds
   and runs without any solution file (SDK 9.0.200+ even reads `.slnx`;
   on an 8.x SDK target the project directly):

   ```powershell
   dotnet build BarangayDocumentSystem/BarangayDocumentSystem.csproj
   dotnet run   --project BarangayDocumentSystem
   ```

   `global.json` pins SDK 8.0.100 with `rollForward: latestMajor`, so any
   newer SDK you already have is fine.
6. **Safety check:** if *Windows Explorer* (not Visual Studio) shows the
   repo folder empty or half-empty after a cleanup, STOP and run
   `git status` then `git checkout . && git pull` - tracked files are
   always restorable, untracked ones are not.

**The `.sln` fallback** sits beside the `.slnx` with the same two projects
and the same GUIDs; it opens on every Visual Studio version ever. One or
the other, never both at once.
