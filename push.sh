#!/usr/bin/env bash
#
# My push helper.
#
# I cannot push from where I build this - there are no GitHub credentials in
# my sandbox, and I would not want a token pasted into a chat log anyway. So I
# prepared the commit and left the actual push to be run by me on my own
# machine, where my credentials already live.
#
#   bash push.sh
#
set -e

REMOTE="https://github.com/cgado2004/Project_BarangayDocumentSystem.git"
BRANCH="leader_draft"
LOCAL="v3-on-leader"

echo "Pushing $LOCAL  ->  $REMOTE  ($BRANCH)"
echo

# I make sure the remote is set up, without failing if it already is.
git remote get-url origin >/dev/null 2>&1 || git remote add origin "$REMOTE"

# I fetch first so I can see whether anybody else pushed while I was working.
git fetch origin "$BRANCH"

# This is the important check. If my branch is not a fast-forward of the
# remote, somebody has pushed since I started and I must NOT overwrite them.
if git merge-base --is-ancestor "origin/$BRANCH" "$LOCAL"; then
    echo "Fast-forward confirmed - nobody else's work will be lost."
    git push origin "$LOCAL:$BRANCH"
    echo
    echo "Done. https://github.com/cgado2004/Project_BarangayDocumentSystem/tree/$BRANCH"
else
    echo "STOP. origin/$BRANCH has commits that $LOCAL does not."
    echo "Somebody pushed while I was working. Pushing now would either be"
    echo "rejected or, with --force, would destroy their work."
    echo
    echo "What I should do instead:"
    echo "    git pull --rebase origin $BRANCH"
    echo "    # fix any conflicts, then run this script again"
    exit 1
fi
