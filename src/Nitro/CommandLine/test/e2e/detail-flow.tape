# End-to-end recording of the Tasks tab's task detail view and dependency
# tree explorer against the fixture workspace, launched via bare
# `nitro agent` so the tab strip is on screen
# throughout, same as board-flow.tape/search-flow.tape.
#
# There is no dedicated interactive "detail" entry command
# (AgentTuiLauncher is the TuiShell construction site, see
# agent-root-flow.tape's header); the detail view and tree explorer are both
# reached through search mode, same as search-flow.tape: '/' into search,
# Tab Tab from the query input to the detail pane. From there this flow
# exercises what search-flow.tape does not: the detail body's dependency
# section, entering the dependency tree explorer ('t',
# TuiMessage.OpenTreeRequested), toggling its direction ('d') to reveal the
# seeded acme-epic1 children, drilling
# into one of them (Enter, DependencyTreeView.Refocus, pushing a
# breadcrumb), and navigating back up the breadcrumb stack ('u',
# TuiMessage.NavigateTreeBack) -- the tree explorer's own back gesture, not
# Escape/TuiMessage.Back (which would exit the tree entirely, back to
# search).
#
# acme-epic1 is the root: fixtures/seed.sql gives it two parent-child
# children (acme-epic1.1, acme-epic1.2) and no outgoing dependencies of its
# own, so its detail body's "Blocks:" section (TaskDetailModel.Rows pairs
# outgoing Dependencies with incoming Blocks; a parent-child edge counts as
# incoming here, see TaskDetailModel.BuildRows/GetDependentsAsync) lists
# exactly those two children, and it is also the fixture task whose Created
# and Updated sidebar lines both land on 2026-01-01 (it was created and
# never touched again), the deterministic marker this flow's frame
# extraction keys on.
#
# The dependency tree explorer defaults to TreeEdgeMode.Blocking (which
# includes parent-child edges, TaskDependencyTypes.IsBlocking) and
# TreeDirection.Up ("depends on"); acme-epic1 has nothing to depend on, so
# the tree opens on a lone root row until 'd' toggles direction to "depended
# on by", which is when its children appear.
#
# Fixed geometry (Width/Height below) is the stability lever for this
# full-screen layout, same as board-flow.tape/search-flow.tape: changing it
# requires `--update` of every board-flow*/search-flow*/detail-flow* golden.
# Produces detail-flow.gif + detail-flow.txt.

Output detail-flow.gif
Output detail-flow.txt

Set Shell bash
Set FontSize 18
Set Width 1100
Set Height 850
Set Padding 14
Set TypingSpeed 50ms
Set CursorBlink false

# --- setup (hidden) ---
Hide
Type `export PS1="$ "` Enter
Type `export PATH=/src/src/Nitro/CommandLine/test/e2e/bin:$PATH` Enter
Type `export HOME=/tmp/home` Enter
Type `export NITRO_TASK_ACTOR=e2e-agent` Enter
Type `mkdir -p "$HOME"` Enter
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cp -r /vhs/fixture/acme /tmp/work/acme && cd /tmp/work/acme` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent` Enter

# Board's initial frame (same sentinel as board-flow.tape).
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# '/' into search mode; the pending empty query resolves to all six fixture
# tasks on the next tick, ordered by priority then created_at, selection on
# the first row (acme-a1b).
Type "/"
Wait+Screen@30s /6 task\(s\)/

# Tab into the results list, then Down once: acme-a1b (P0) -> acme-epic1
# (P1), the next row by priority.
Tab
Down
# The Results panel wraps this row's trailing "page" word onto its own line
# at this fixed geometry, so the wait anchors on the row's text up to the
# wrap point rather than the full title (see search-flow.tape).
Wait+Screen@30s /> ○ \[E\] P1 acme-epic1 Launch v2 pricing/

# Tab again: List -> Detail focus, landing on acme-epic1's detail pane. Its
# body has no Description/Design/Acceptance criteria/Notes/Comments (all
# empty in the fixture), so the "Blocks:" section -- its two parent-child
# children -- is visible without scrolling. Detail focus puts the selection
# cursor on the first Blocks row; at this fixed geometry the body panel is
# narrow enough that the row's target id/title wrap onto their own line
# (see search-flow.tape), so the wait anchors on the unwrapped prefix +
# dependency type.
Tab
Wait+Screen@30s /> ● parent-child <-/

# 't' opens the dependency tree explorer on the selected task (acme-epic1).
# Default Up direction ("depends on") shows only the root: acme-epic1 has no
# outgoing dependency edges. 'd' toggles to Down ("depended on by"),
# revealing its two seeded children.
Type "t"
Type "d"
Wait+Screen@30s /acme-epic1 · blocking · depended on by/

# Down selects the first child (acme-epic1.1); Enter refocuses the tree on
# it (DependencyTreeView.Refocus), pushing acme-epic1 onto the breadcrumb
# stack. Its own Down tree (still Blocking/Down) shows acme-epic1.2, which
# blocks-depends-on acme-epic1.1 per fixtures/seed.sql, and transitively
# acme-a1b beneath it. The panel header carries the breadcrumb, but at this
# fixed geometry the two-id breadcrumb text overflows the header and gets
# ellipsis-truncated by Spectre's PanelHeader before it reaches " by", so the
# wait anchors on the unwrapped, untruncated root row instead.
Down
Enter
Wait+Screen@30s /> ● \[T\] P2 acme-epic1\.1 Draft new pricing copy/

# 'u' pops the breadcrumb stack (DependencyTreeView.NavigateBack), back to
# the tree rooted on acme-epic1 with its two children again -- the tree
# explorer's own back gesture, distinct from Escape/exiting to search.
# Final state for the golden.
Type "u"
Wait+Screen@30s /acme-epic1 · blocking · depended on by/

Sleep 1500ms
Screenshot detail-flow-final.png
Sleep 500ms
