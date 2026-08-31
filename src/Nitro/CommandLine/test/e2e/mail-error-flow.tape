# End-to-end recording of a failing `nitro agent mail send` in a fresh
# agent workspace (initialized via the unified `nitro agent init`).
#
# Sending to a recipient that was never registered is no longer an error: an
# unknown recipient gets an implicit mailbox (MailStore.EnsureRecipientsAsync)
# and the send still succeeds with a "note: '<name>' has never registered."
# warning. The one remaining hard-fail on `mail send` is an INVALID
# recipient name: MailAgentName.Normalize rejects any character
# outside lowercase letters, digits, `-`, and `_` with an ExitException,
# caught by SetActionWithExceptionHandling for exit code 1, before the
# sender is ever touched by the registry (BuildRecipients normalizes
# recipients ahead of AgentRegistry.TouchAsync, so a rejected recipient name
# leaves the sender unregistered too). This mirrors error-flow.tape's
# "[nitro exit: $?]" trick since VHS only asserts on rendered screen text,
# never on a process exit code. The invalid recipient name is static,
# non-dynamic content, so no SCRUBS entry is needed. Needs no auth: mail
# commands do not call the Nitro backend.
# Produces mail-error-flow.gif + mail-error-flow.txt.

Output mail-error-flow.gif
Output mail-error-flow.txt

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
Type `nitro agent mail send --to "not.a.valid.agent" --subject "Status update" --body "All good." --actor e2e-agent; echo "[nitro exit: $?]"` Enter

Wait+Screen@30s /\[nitro exit: 1\]/
Sleep 1500ms
Screenshot mail-error-flow-final.png
Sleep 500ms
