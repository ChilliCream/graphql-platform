# End-to-end recording of `nitro agent tasks dep tree` against the fixture
# workspace.
#
# `dep tree` walks a task's *outgoing* dependencies (what it depends on), not
# an epic's children, so it is run against acme-epic1.2 (fixtures/seed.sql),
# which has both a `parent-child` edge to acme-epic1 and a `blocks` edge to
# acme-epic1.1: enough to render a populated, deterministic tree with a
# repeated node (marked `*`). Needs no auth: task commands do not call the
# Nitro backend.
# Produces dep-tree-flow.gif + dep-tree-flow.txt.

Output dep-tree-flow.gif
Output dep-tree-flow.txt

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
Type `mkdir -p "$HOME"` Enter
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cp -r /vhs/fixture/acme /tmp/work/acme && cd /tmp/work/acme` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent tasks dep tree acme-epic1.2` Enter

Wait+Screen@30s /acme-epic1\.1/
Sleep 1500ms
Screenshot dep-tree-flow-final.png
Sleep 500ms
