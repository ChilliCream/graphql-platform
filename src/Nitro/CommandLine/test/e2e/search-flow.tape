# End-to-end recording of the tasks TUI's search mode against the fixture
# workspace.
#
# Search mode (perles-net-4j7.3) is reached from the board TUI via the '/'
# gesture (TuiMessage.FocusSearchRequested, KeyMap.CreateDefaultGlobal): it
# is the only entry point, there is no dedicated interactive search command
# (`nitro agent tasks search` is a separate, non-interactive text-search command
# that never touches Tui/Search/SearchMode.cs). Drives the query input's
# live-filtering (TaskQueryParser/TaskQuery.ToFilter against
# fixtures/seed.sql) through one incremental narrowing step, then Tab
# through Input -> List -> Detail focus (SearchMode.HandleOpenSelected) to
# land on the selected task's detail pane, syncing every transition on a
# Wait+Screen sentinel anchored on fixture task titles, ids, or the live
# result count, never on Sleep.
#
# `status:open` alone matches three fixture tasks (acme-a1b P0, acme-epic1
# P1, acme-epic1.2 P2: the three open tasks per fixtures/seed.sql), ordered
# by priority. Extending it to `status:open pricing` narrows the free-text
# `Text` filter (title/description/design/acceptance-criteria/notes,
# LIKE-matched, see TaskStore's BuildWhereClause) down to exactly one:
# acme-epic1's title is the only one of the three containing "pricing"
# ("Launch v2 pricing page"; acme-epic1.2 is "Wire up billing API", acme-a1b
# is "Checkout crashes on Safari").
#
# Fixed geometry (Width/Height below) is the stability lever for this
# full-screen layout, same as board-flow.tape: changing it requires
# `--update` of every board-flow*/search-flow*/detail-flow* golden.
# Produces search-flow.gif + search-flow.txt.

Output search-flow.gif
Output search-flow.txt

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
Type `nitro agent tasks board` Enter

# Board's initial frame (same sentinel as board-flow.tape): focus starts on
# "Blocked", selection on its first row, acme-a1b.
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# '/' jumps into search mode (TuiShell.HandleMessage FocusSearchRequested),
# focus on the query input. The pending query from mode entry (TaskQuery.Empty)
# resolves on the next tick to all six fixture tasks, ordered by priority
# then created_at, selection on the first row (acme-a1b).
Type "/"
Wait+Screen@30s /6 task\(s\)/

# Incremental step 1: "status:open" narrows to three fixture hits.
Type `status:open`
Wait+Screen@30s /3 task\(s\)/

# Incremental step 2: extending the query with a free-text word narrows to
# exactly one hit.
Type ` pricing`
# Focus is still on the query input (no Tab yet), so the Results list has no
# selection cursor to anchor on (SearchResultsList only renders "> " when the
# list itself is focused); the narrowing to exactly one hit is what this step
# exercises, so anchor on the result count, same as the previous step.
Wait+Screen@30s /1 task\(s\)/

# Tab moves focus Input -> List (SearchMode's own Tab binding mirrors
# Enter/OpenSelected); a second Tab moves List -> Detail, landing on the
# selected task's detail pane. Detail focus puts the selection cursor on the
# body's first Blocks row (TaskDetailBody.BuildRowSection, selected when
# focused && row.Index == SelectedRowIndex); at this fixed geometry the body
# panel is narrow enough that the row's target id/title wrap onto their own
# line (TaskDetailRowRenderer's single logical line gets soft-wrapped by
# Spectre), so the wait anchors on the unwrapped prefix + dependency type.
# Final state for the golden.
Tab
Tab
Wait+Screen@30s /> ● parent-child <-/

Sleep 1500ms
Screenshot search-flow-final.png
Sleep 500ms
