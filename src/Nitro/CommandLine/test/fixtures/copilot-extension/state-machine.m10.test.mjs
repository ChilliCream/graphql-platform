// M10 settlement test (perles-net-k3j.16): repeated sends across watcher
// restart boundaries all produce at least one session.send, including mail
// accumulated before the extension ever started. Drives the pure state
// machine exported by the shipped extension asset directly (no Copilot SDK,
// no real `nitro` process, no file I/O) - restart is simulated by throwing
// away in-memory state and rebuilding it from only what a real restart could
// recover: the persisted cursor.
//
// Invoked from CopilotExtensionStateMachineM10Tests (test/CommandLine.Tests)
// via `node --test`, not run standalone as part of any C# test discovery.

import test from 'node:test';
import assert from 'node:assert/strict';
import {
  createInitialState,
  onMailObserved,
  onSessionStart,
  onUserPromptSubmitted,
  onAgentStop,
  onSessionEnd,
  planFlush,
  afterFlushSucceeded,
  Phase,
} from '../../../src/CommandLine/Assets/CopilotExtension/extension.mjs';

/**
 * Simulates one full extension process lifetime: seeds state from
 * `persistedCursor` (what a restart would read off disk; null on the very
 * first-ever run), replays `events`, and returns every `session.send` this
 * lifetime performed plus the cursor it persisted (or null if it never
 * flushed). `events` is a list of either:
 *   - `{ mail: MailEntry[] }` - a `mail watch` poll returning these messages
 *   - `'sessionStart' | 'agentStop' | 'userPromptSubmitted' | 'sessionEnd'`
 * A crash mid-lifetime is modeled by simply stopping the replay early: the
 * next call's `persistedCursor` argument is whatever the crashed lifetime
 * actually persisted (possibly still null), never something it merely
 * intended to persist.
 */
function runLifetime(persistedCursor, events) {
  let state = createInitialState();
  state = { ...state, cursor: persistedCursor };
  const sends = [];

  function tryFlush() {
    const plan = planFlush(state);
    if (!plan) {
      return;
    }
    state = plan.state;
    sends.push(plan.messages.map((m) => m.id));
    state = afterFlushSucceeded(state, plan.messages);
  }

  for (const event of events) {
    if (typeof event === 'object') {
      state = onMailObserved(state, event.mail);
      tryFlush();
      continue;
    }
    switch (event) {
      case 'sessionStart':
        state = onSessionStart(state);
        tryFlush();
        break;
      case 'agentStop':
        state = onAgentStop(state, false);
        tryFlush();
        break;
      case 'userPromptSubmitted':
        state = onUserPromptSubmitted(state);
        break;
      case 'sessionEnd':
        state = onSessionEnd(state);
        break;
      default:
        throw new Error(`unknown event: ${event}`);
    }
  }

  return { sends, persistedCursor: state.cursor };
}

test('mail accumulated before the extension ever starts is flushed on first idle', () => {
  const preexisting = [
    { id: 'm1', from: 'agent-a', createdAt: '2026-01-01T00:00:00Z' },
    { id: 'm2', from: 'agent-b', createdAt: '2026-01-01T00:00:01Z' },
  ];

  // No persisted cursor: this is the extension's first-ever run. The
  // `--include-existing` poll surfaces mail that predates the extension's
  // own startup through the exact same onMailObserved call a `--after`
  // poll would use for brand-new mail - the state machine cannot tell the
  // two apart, which is the point.
  const result = runLifetime(null, ['sessionStart', { mail: preexisting }]);

  assert.deepEqual(result.sends, [['m1', 'm2']]);
  assert.deepEqual(result.persistedCursor, { id: 'm2', createdAt: '2026-01-01T00:00:01Z' });
});

test('a restart after mail arrived but before it was flushed still flushes it', () => {
  // Lifetime 1: mail arrives while busy, so it cannot flush yet. The
  // process is killed here (simulated by just not calling agentStop) -
  // nothing was ever sent, so nothing was ever persisted.
  const lifetime1 = runLifetime(null, [
    'sessionStart',
    'userPromptSubmitted',
    { mail: [{ id: 'm1', from: 'agent-a', createdAt: '2026-01-01T00:00:00Z' }] },
  ]);

  assert.deepEqual(lifetime1.sends, [], 'busy session must not flush');
  assert.equal(lifetime1.persistedCursor, null, 'nothing was ever persisted');

  // Lifetime 2 (post-restart): starts from the same null cursor a real
  // restart would recover (the crash happened before any cursor write), so
  // its first `mail watch` call uses --include-existing again and
  // re-observes m1. This is the restart-boundary guarantee: at least one
  // session.send happens for m1 across the two lifetimes combined.
  const lifetime2 = runLifetime(lifetime1.persistedCursor, [
    'sessionStart',
    { mail: [{ id: 'm1', from: 'agent-a', createdAt: '2026-01-01T00:00:00Z' }] },
  ]);

  assert.deepEqual(lifetime2.sends, [['m1']]);
});

test('a restart after a successful flush does not resend the same mail', () => {
  const lifetime1 = runLifetime(null, [
    'sessionStart',
    { mail: [{ id: 'm1', from: 'agent-a', createdAt: '2026-01-01T00:00:00Z' }] },
  ]);

  assert.deepEqual(lifetime1.sends, [['m1']]);
  assert.ok(lifetime1.persistedCursor);

  // Post-restart lifetime starts from the advanced cursor. A `mail watch
  // --after <cursor>` poll would not re-surface m1 at all (that filtering
  // happens on the real CLI side, out of scope for this pure-logic test),
  // so the state machine here simply never receives it again.
  const lifetime2 = runLifetime(lifetime1.persistedCursor, ['sessionStart']);

  assert.deepEqual(lifetime2.sends, []);
});

test('repeated restart boundaries each still produce at least one send for newly arrived mail', () => {
  let cursor = null;
  const allSends = [];

  for (let i = 0; i < 5; i++) {
    const messageId = `m${i}`;
    const result = runLifetime(cursor, [
      'sessionStart',
      { mail: [{ id: messageId, from: 'agent-a', createdAt: `2026-01-01T00:00:0${i}Z` }] },
    ]);

    assert.equal(result.sends.length, 1, `lifetime ${i} must send exactly once`);
    assert.deepEqual(result.sends[0], [messageId]);

    allSends.push(...result.sends);
    cursor = result.persistedCursor;
  }

  assert.equal(allSends.length, 5);
});

test('injection-loop guard: draining blocks a second overlapping flush attempt', () => {
  let state = createInitialState();
  state = onSessionStart(state);
  state = onMailObserved(state, [{ id: 'm1', from: 'agent-a', createdAt: '2026-01-01T00:00:00Z' }]);

  const firstPlan = planFlush(state);
  assert.ok(firstPlan, 'idle session with pending mail must plan a flush');
  state = firstPlan.state;
  assert.equal(state.phase, Phase.DRAINING);

  // While the first send is still in flight, the digest's own prompt lands
  // back through the hooks (this is the loop risk): userPromptSubmitted
  // must not flip this back to BUSY in a way that would later re-arm a
  // second flush once agentStop fires for the SAME turn, and a second
  // planFlush call right now must refuse (there is nothing new pending
  // beyond what is already draining, and the phase is not IDLE).
  state = onUserPromptSubmitted(state);
  assert.equal(state.phase, Phase.DRAINING, 'a prompt landing mid-drain must not leave DRAINING');

  const secondPlanWhileDraining = planFlush(state);
  assert.equal(secondPlanWhileDraining, null, 'must not plan a second overlapping flush');

  state = onAgentStop(state, false);
  assert.equal(state.phase, Phase.DRAINING, 'agentStop for the digest\'s own turn must not exit DRAINING early');

  state = afterFlushSucceeded(state, firstPlan.messages);
  assert.equal(state.pending.length, 0);

  // Only once the extension itself transitions the phase after a
  // completed send (mirroring what `tryFlush` does at runtime by then
  // waiting for the NEXT real agentStop) can a following flush be planned.
  state = { ...state, phase: Phase.IDLE };
  state = onMailObserved(state, [{ id: 'm2', from: 'agent-a', createdAt: '2026-01-01T00:00:01Z' }]);

  const thirdPlan = planFlush(state);
  assert.ok(thirdPlan, 'a later, genuinely new message must still be flushable');
  assert.deepEqual(thirdPlan.messages.map((m) => m.id), ['m2']);
});

test('sessionEnd resets to RESTART and a fresh sessionStart returns to IDLE without resending flushed mail', () => {
  let state = createInitialState();
  state = onSessionStart(state);
  state = onMailObserved(state, [{ id: 'm1', from: 'agent-a', createdAt: '2026-01-01T00:00:00Z' }]);

  const plan = planFlush(state);
  state = afterFlushSucceeded(plan.state, plan.messages);

  state = onSessionEnd(state);
  assert.equal(state.phase, Phase.RESTART);

  state = onSessionStart(state);
  assert.equal(state.phase, Phase.IDLE);
  assert.equal(planFlush(state), null, 'no pending mail left to flush after a clean session end/start cycle');
});
