# Claude Code hooks fixtures

Unlike the sibling `codex/` and `copilot/` fixtures, these are
hand-authored, not captured live. This ticket (perles-net-k3j) is
barred from installing hooks into `~/.claude`; a live capture round
trip is out of scope here and belongs to the `.8` installer follow-up.

The payload shapes are built against Claude Code 2.1.226's documented
hook schema and cross-checked against the strings embedded in the
`claude` binary itself (field names, event names, and the
`hookSpecificOutput`/`hookEventName`/`additionalContext` response
envelope), not observed on the wire. Treat any test naming or claim
that these were "captured" as a documentation bug to fix, not as
evidence they were.
