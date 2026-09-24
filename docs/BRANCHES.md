# Branch map — where the project exists, and the consolidation plan

The repository currently carries the project as **seven branch variants**.
On GitHub's branch dropdown this reads as "duplicated projects" — it is the
team's working history, not copied files inside any one branch. The branch
that matters for the final submission is `arena/01a0cf05-project-barangaydocumentsystem`
(the composite; PR #3 merges it into `leader_draft`).

| Branch | Tip | What it holds | Status |
| --- | --- | --- | --- |
| `arena/01a0cf05…` | `62f6c87`+ | **The composite**: Draft UI + Draft2 MySQL + Charter fees, extracted to root, net4.8 | the one true build |
| `leader_draft` | `87926d9` | v3.1 single project (`.slnx` + `BarangayDocumentSystem/` + `tests/RuleChecks`) | superseded by the composite |
| `master` | `ae9a641` | old 3-project layout (`src/Domain`, `src/Infrastructure`, `src/UI`) | superseded; original upload history |
| `draft3` | `5a6e501` | same `src/` 3-project layout (Phillippe's) | superseded; untouched by agreement |
| `Draft` | `77485c6` | Jonathan's original (net4.7.2, LocalDB) — UI lives on inside the composite | superseded by the composite |
| `Draft2` | `12c37db` | Frent's original MySQL app — DB layer lives on inside the composite | superseded by the composite (do not delete without Frent's OK) |
| `Document` | `348d859` | **EMPTY** — every file deleted (`src/`, docs, sln, README all removed) | stray; confirm intent, then delete or reuse |

## Consolidation (owner-run, from a machine with push rights)

```bash
git fetch --all
# 1. the composite becomes the unified line
git branch unified origin/arena/01a0cf05-project-barangaydocumentsystem
git push origin unified
# 2. after the team agrees, retire the superseded branches
git push origin --delete Draft Draft2 draft3 master Document
#    (keep leader_draft as the long-lived main; PR #3 merges the composite into it)
```

Nothing outside the arena branch is touched from the working session.
