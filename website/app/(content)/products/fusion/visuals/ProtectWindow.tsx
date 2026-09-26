"use client";

import { useRef } from "react";

import { AppWindow } from "@/src/components/AppWindow";
import { BlockMark } from "@/src/icons/BlockMark";
import { CheckGlyph } from "@/src/icons/CheckGlyph";

import { useElementMotion } from "./hooks";
import {
  CLIENT_OPS,
  MOBILE_IMPACT,
  MOBILE_IMPACT_COMPACT,
  SCHEMA_FILE,
  V14,
  V15,
} from "./protect/data";
import {
  ClientRow,
  DockLabel,
  RegistryNode,
  ScanBar,
  TravelCard,
} from "./protect/parts";

/**
 * Matches the sitewide 11s "beat" loop length (`src/nitro/lib/motion.tsx`'s
 * `beat.loop`); the second document's travel is this shared cycle's own
 * timeline shifted by exactly half a period, so one set of keyframes plays
 * twice per loop without duplicating them.
 */
const CYCLE_SECONDS = 11;
const HALF = CYCLE_SECONDS / 2;
const CYCLE_MS = CYCLE_SECONDS * 1000;

/** Fixed row height (matches `ClientRow`'s `h-9`) so the scan's travel distance is exact, not measured. */
const ROW_HEIGHT_REM = 2.25;
const TABLE_HEIGHT_REM = CLIENT_OPS.length * ROW_HEIGHT_REM;

/**
 * The wide (horizontal) lane's travel distance, in cqi (the `.pw-root`
 * container's inline size) minus the fixed-width chrome around it: the
 * row's own `px-4` (16px) padding on each side, the registry node's
 * rendered width (135px), the card's own width at this breakpoint (11.5rem
 * = 184px, the only one that applies once the lane is wide enough to show),
 * and an 8px clearance so the card docks beside the registry, not under it.
 */
const TRAVEL_X = "calc(100cqi - 359px)";

/**
 * The stacked (narrow) lane's fixed height and the card's travel distance
 * within it: the card's own rendered height (with a wrapped verdict) runs up
 * to about 9rem, so the lane and travel distance below leave it fully
 * contained, never reaching the registry node or the table beneath it.
 */
const VLANE_HEIGHT_REM = 12;
const VLANE_TRAVEL_REM = 2;

/**
 * Row-check reveal delays, keyed to when the (linear) scan bar actually
 * crosses that row's bottom edge, plus a small safety margin so the check
 * never renders before its row has been scanned.
 */
const SCAN_START_PCT = 20;
const SCAN_END_PCT = 38;
/** `pw-check`'s own local reveal point below, kept in sync with it. */
const CHECK_LOCAL_REVEAL_PCT = 0.5;
const CHECK_MARGIN_MS = 80;

function checkDelay(rowIndex: number): number {
  const crossingMs =
    ((SCAN_START_PCT +
      ((SCAN_END_PCT - SCAN_START_PCT) * (rowIndex + 1)) / CLIENT_OPS.length) /
      100) *
    CYCLE_MS;
  const localRevealMs = (CHECK_LOCAL_REVEAL_PCT / 100) * CYCLE_MS;
  return (crossingMs + CHECK_MARGIN_MS - localRevealMs) / 1000;
}

const KEYFRAMES = `
.pw-anim {
  animation-duration: ${CYCLE_SECONDS}s;
  animation-timing-function: ease-in-out;
  animation-iteration-count: infinite;
  animation-play-state: running;
  /* Without this, an instance with a positive delay (the second document)
     renders at its default (visible) style during the wait before its
     first iteration instead of the keyframe's 0% state. */
  animation-fill-mode: both;
}
.pw-root[data-motion="paused"] .pw-anim {
  animation-play-state: paused;
}
.pw-scan-el {
  /* Linear so the row-check delays above can key to an exact crossing time. */
  animation-timing-function: linear;
}
@keyframes pw-doc-travel-x {
  0% { opacity: 0; transform: translateX(0); }
  4% { opacity: 1; transform: translateX(0); }
  18% { opacity: 1; transform: translateX(${TRAVEL_X}); }
  46% { opacity: 1; transform: translateX(${TRAVEL_X}); }
  50% { opacity: 0; transform: translateX(${TRAVEL_X}); }
  100% { opacity: 0; transform: translateX(0); }
}
@keyframes pw-doc-travel-y {
  0% { opacity: 0; transform: translateY(0); }
  4% { opacity: 1; transform: translateY(0); }
  18% { opacity: 1; transform: translateY(${VLANE_TRAVEL_REM}rem); }
  46% { opacity: 1; transform: translateY(${VLANE_TRAVEL_REM}rem); }
  50% { opacity: 0; transform: translateY(${VLANE_TRAVEL_REM}rem); }
  100% { opacity: 0; transform: translateY(0); }
}
@keyframes pw-scan {
  0%, 18% { opacity: 0; transform: translateY(0); }
  20% { opacity: 1; transform: translateY(0); }
  38% { opacity: 1; transform: translateY(${TABLE_HEIGHT_REM}rem); }
  40%, 68% { opacity: 0; }
  70% { opacity: 1; transform: translateY(0); }
  88% { opacity: 1; transform: translateY(${TABLE_HEIGHT_REM}rem); }
  90%, 100% { opacity: 0; transform: translateY(0); }
}
@keyframes pw-check {
  0% { opacity: 0; }
  0.5% { opacity: 1; }
  13% { opacity: 1; }
  14%, 100% { opacity: 0; }
}
@keyframes pw-stamp {
  0%, 41% { opacity: 0; }
  44%, 47% { opacity: 1; }
  50%, 100% { opacity: 0; }
}
`;

/** Outcome copy for one client operation on the breaking (v14) pass. */
function firstOutcome(id: string, delay: number) {
  if (id === "mobile") {
    return { tone: "danger" as const, text: MOBILE_IMPACT_COMPACT, delay };
  }
  return { tone: "success" as const, text: "ok", delay };
}

/** Outcome copy for one client operation on the safe (v15) pass. */
function secondOutcome(delay: number) {
  return { tone: "success" as const, text: "ok", delay };
}

/**
 * The "Protect your clients" row's graphic: a schema document travels into
 * the Nitro registry, is analyzed against the operations registered clients
 * use, fails because mobile still queries a removed field, then a second
 * document that deprecates the field instead passes and is published. Loops
 * calmly while in view; `prefers-reduced-motion` renders one complete static
 * frame with both outcomes.
 */
export function ProtectWindow() {
  const rootRef = useRef<HTMLDivElement>(null);
  const running = useElementMotion(rootRef);

  return (
    <AppWindow
      title={
        <span className="text-cc-prose">nitro registry · publish check</span>
      }
      footer={
        <span className="text-cc-ink-dim font-mono text-[0.7rem]">
          every change checked against registered client operations
        </span>
      }
    >
      <div
        ref={rootRef}
        className="pw-root @container"
        data-motion={running ? "running" : "paused"}
      >
        <style>{KEYFRAMES}</style>

        <div className="motion-reduce:hidden">
          <div className="relative hidden min-h-52 items-center gap-3 px-4 pt-4 @[480px]:flex">
            <DockLabel />
            <span
              aria-hidden="true"
              className="border-cc-card-border/70 h-px flex-1 border-t border-dashed"
            />
            <RegistryNode />
            <TravelCard
              axis="x"
              version={V14.version}
              diff={V14.diff}
              diffTone="danger"
              verdict={V14.verdict}
              verdictTone="danger"
              schemaFile={SCHEMA_FILE}
              delay={0}
            />
            <TravelCard
              axis="x"
              version={V15.version}
              diff={V15.diff}
              diffTone="warning"
              verdict={V15.verdict}
              verdictTone="success"
              schemaFile={SCHEMA_FILE}
              delay={HALF}
            />
          </div>

          <div className="flex flex-col items-center gap-2 px-4 pt-4 @[480px]:hidden">
            <DockLabel />
            <div
              className="relative w-full"
              style={{ height: `${VLANE_HEIGHT_REM}rem` }}
            >
              <span
                aria-hidden="true"
                className="border-cc-card-border/70 absolute inset-y-0 left-1/2 w-px -translate-x-1/2 border-l border-dashed"
              />
              <TravelCard
                axis="y"
                version={V14.version}
                diff={V14.diff}
                diffTone="danger"
                verdict={V14.verdict}
                verdictTone="danger"
                schemaFile={SCHEMA_FILE}
                delay={0}
              />
              <TravelCard
                axis="y"
                version={V15.version}
                diff={V15.diff}
                diffTone="warning"
                verdict={V15.verdict}
                verdictTone="success"
                schemaFile={SCHEMA_FILE}
                delay={HALF}
              />
            </div>
            <RegistryNode />
          </div>

          <div className="px-4 pb-4">
            <div className="border-cc-card-border relative overflow-hidden rounded-lg border">
              <ScanBar />
              {CLIENT_OPS.map((op, i) => (
                <ClientRow
                  key={op.id}
                  label={op.label}
                  first={firstOutcome(op.id, checkDelay(i))}
                  second={secondOutcome(checkDelay(i) + HALF)}
                />
              ))}
            </div>
          </div>
        </div>

        <div className="hidden space-y-2 px-4 py-4 motion-reduce:block">
          <div className="border-cc-danger/40 bg-cc-danger/[0.06] flex items-start gap-2 rounded-lg border px-3 py-2.5">
            <BlockMark
              width={13}
              height={13}
              className="text-cc-danger mt-0.5 shrink-0"
            />
            <div className="min-w-0">
              <div className="text-cc-heading font-mono text-[0.7rem]">
                {SCHEMA_FILE} · {V14.version}
              </div>
              <div className="text-cc-danger mt-0.5 font-mono text-[0.7rem]">
                {V14.diff} · mobile still used · {MOBILE_IMPACT}
              </div>
              <div className="text-cc-danger mt-0.5 font-mono text-[0.7rem] font-semibold">
                {V14.verdict}
              </div>
            </div>
          </div>
          <div className="border-cc-success/40 bg-cc-success/[0.06] flex items-start gap-2 rounded-lg border px-3 py-2.5">
            <CheckGlyph
              width={13}
              height={13}
              className="text-cc-success mt-0.5 shrink-0"
            />
            <div className="min-w-0">
              <div className="text-cc-heading font-mono text-[0.7rem]">
                {SCHEMA_FILE} · {V15.version}
              </div>
              <div className="text-cc-ink-dim mt-0.5 font-mono text-[0.7rem]">
                {V15.diff} · every client compatible
              </div>
              <div className="text-cc-success mt-0.5 font-mono text-[0.7rem] font-semibold">
                {V15.verdict}
              </div>
            </div>
          </div>
        </div>
      </div>
    </AppWindow>
  );
}
