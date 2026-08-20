# End-to-end recording of a failing `nitro agent mail send` in a fresh mail
# workspace.
#
# Exercises the non-zero-exit UX for sending to a recipient that was never
# registered (MailStore.ValidateRecipientsExistAsync -> ExitException, caught
# by SetActionWithExceptionHandling, exit code 1), mirroring error-flow.tape's
# "[nitro exit: $?]" trick since VHS only asserts on rendered screen text,
# never on a process exit code. The sender auto-registers on send
# (MailStore.SendMessageAsync calls RegisterAgentAsync inline), so this needs
# no separate `register` step; only the unregistered recipient name is
# static, non-dynamic content, so no SCRUBS entry is needed. Needs no auth:
# mail commands do not call the Nitro backend.
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
Type `export NITRO_MAIL_ACTOR=agent-a` Enter
Type `mkdir -p "$HOME"` Enter
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cd /tmp/work` Enter
Type `nitro agent mail init >/dev/null` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent mail send nope-agent --subject "Status update" --body "All good."; echo "[nitro exit: $?]"` Enter

Wait+Screen@30s /\[nitro exit: 1\]/
Sleep 1500ms
Screenshot mail-error-flow-final.png
Sleep 500ms
