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
/** Extra stagger between rows so their outcome lands as the scan reaches them. */
const STAGGER = 0.2;

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
@keyframes pw-doc-travel {
  0% { opacity: 0; left: 4%; }
  4% { opacity: 1; left: 4%; }
  20% { opacity: 1; left: 46%; }
  46% { opacity: 1; left: 46%; }
  50% { opacity: 0; left: 46%; }
  100% { opacity: 0; left: 4%; }
}
@keyframes pw-scan {
  0%, 18% { opacity: 0; top: 0%; }
  20% { opacity: 1; top: 0%; }
  38% { opacity: 1; top: 100%; }
  40%, 68% { opacity: 0; }
  70% { opacity: 1; top: 0%; }
  88% { opacity: 1; top: 100%; }
  90%, 100% { opacity: 0; top: 0%; }
}
@keyframes pw-check {
  0%, 15% { opacity: 0; }
  18%, 42% { opacity: 1; }
  46%, 100% { opacity: 0; }
}
@keyframes pw-stamp {
  0%, 30% { opacity: 0; }
  34%, 46% { opacity: 1; }
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
          <div className="relative flex h-28 items-center gap-3 px-4 pt-4">
            <DockLabel />
            <span
              aria-hidden="true"
              className="border-cc-card-border/70 h-px flex-1 border-t border-dashed"
            />
            <RegistryNode />
            <TravelCard
              version={V14.version}
              diff={V14.diff}
              diffTone="danger"
              verdict={V14.verdict}
              verdictTone="danger"
              schemaFile={SCHEMA_FILE}
              delay={0}
            />
            <TravelCard
              version={V15.version}
              diff={V15.diff}
              diffTone="warning"
              verdict={V15.verdict}
              verdictTone="success"
              schemaFile={SCHEMA_FILE}
              delay={HALF}
            />
          </div>

          <div className="px-4 pb-4">
            <div className="border-cc-card-border relative overflow-hidden rounded-lg border">
              <ScanBar />
              {CLIENT_OPS.map((op, i) => (
                <ClientRow
                  key={op.id}
                  label={op.label}
                  first={firstOutcome(op.id, i * STAGGER)}
                  second={secondOutcome(HALF + i * STAGGER)}
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
