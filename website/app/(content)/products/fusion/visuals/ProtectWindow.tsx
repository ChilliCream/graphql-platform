"use client";

import { useRef } from "react";

import { AppWindow } from "@/src/components/AppWindow";

import { useElementMotion } from "./hooks";
import {
  CLIENT_OPS,
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
  StaticClientRow,
  StaticVerdictCard,
  TravelCard,
} from "./protect/parts";

const CYCLE_SECONDS = 11;
const HALF = CYCLE_SECONDS / 2;
const CYCLE_MS = CYCLE_SECONDS * 1000;

const ROW_HEIGHT_REM = 2.25;
const TABLE_HEIGHT_REM = CLIENT_OPS.length * ROW_HEIGHT_REM;

const VLANE_HEIGHT_REM = 12;
const VLANE_TRAVEL_REM = 2;

const SCAN_START_PCT = 20;
const SCAN_END_PCT = 38;
const ARRIVE_PCT = 4;
const CHECK_REVEAL_PCT = 0.5;
const CHECK_MARGIN_MS = 80;
const STAMP_GONE_PCT = 50;

function checkDelay(rowIndex: number): number {
  const crossingMs =
    ((SCAN_START_PCT +
      ((SCAN_END_PCT - SCAN_START_PCT) * (rowIndex + 1)) / CLIENT_OPS.length) /
      100) *
    CYCLE_MS;
  const revealMs = (CHECK_REVEAL_PCT / 100) * CYCLE_MS;
  return (crossingMs + CHECK_MARGIN_MS - revealMs) / 1000;
}

const CHECK_CLEAR_MARGIN_MS = 400;

const CHECK_GONE_TARGET_S =
  (HALF +
    (HALF +
      (ARRIVE_PCT / 100) * CYCLE_SECONDS -
      CHECK_CLEAR_MARGIN_MS / 1000)) /
  2;

function checkGonePct(rowIndex: number): number {
  return (
    Math.round(
      ((CHECK_GONE_TARGET_S - checkDelay(rowIndex)) / CYCLE_SECONDS) * 10000,
    ) / 100
  );
}

function checkKeyframeName(rowIndex: number): string {
  return `pw-check-${rowIndex}`;
}

function checkKeyframe(rowIndex: number): string {
  const gone = checkGonePct(rowIndex);
  const hold = gone - 1;
  return `@keyframes ${checkKeyframeName(rowIndex)} {
  0% { opacity: 0; }
  ${CHECK_REVEAL_PCT}% { opacity: 1; }
  ${hold}% { opacity: 1; }
  ${gone}%, 100% { opacity: 0; }
}`;
}

const KEYFRAMES = `
.pw-anim {
  animation-duration: ${CYCLE_SECONDS}s;
  animation-timing-function: ease-in-out;
  animation-iteration-count: infinite;
  animation-play-state: running;
  /* both: keeps a delayed instance at its keyframe start, not its default style, before it first plays. */
  animation-fill-mode: both;
}
.pw-root[data-motion="paused"] .pw-anim {
  animation-play-state: paused;
}
.pw-scan-el {
  animation-timing-function: linear;
}
@keyframes pw-doc-travel-x {
  0% { opacity: 0; transform: translateX(0%); }
  ${ARRIVE_PCT}% { opacity: 1; transform: translateX(0%); }
  18% { opacity: 1; transform: translateX(100%); }
  46% { opacity: 1; transform: translateX(100%); }
  50% { opacity: 0; transform: translateX(100%); }
  100% { opacity: 0; transform: translateX(0%); }
}
@keyframes pw-doc-travel-x-counter {
  0% { transform: translateX(0%); }
  ${ARRIVE_PCT}% { transform: translateX(0%); }
  18% { transform: translateX(-100%); }
  46% { transform: translateX(-100%); }
  50% { transform: translateX(-100%); }
  100% { transform: translateX(0%); }
}
@keyframes pw-doc-travel-y {
  0% { opacity: 0; transform: translateY(0); }
  ${ARRIVE_PCT}% { opacity: 1; transform: translateY(0); }
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
@keyframes pw-stamp {
  0%, 41% { opacity: 0; }
  44%, 47% { opacity: 1; }
  ${STAMP_GONE_PCT}%, 100% { opacity: 0; }
}
${CLIENT_OPS.map((_, rowIndex) => checkKeyframe(rowIndex)).join("\n")}
`;

function firstOutcome(id: string, delay: number, keyframeName: string) {
  if (id === "mobile") {
    return {
      tone: "danger" as const,
      text: MOBILE_IMPACT_COMPACT,
      delay,
      keyframeName,
    };
  }
  return { tone: "success" as const, text: "ok", delay, keyframeName };
}

function secondOutcome(delay: number, keyframeName: string) {
  return { tone: "success" as const, text: "ok", delay, keyframeName };
}

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
            <div className="relative flex-1 self-stretch">
              <span
                aria-hidden="true"
                className="border-cc-card-border/70 absolute top-1/2 right-0 left-0 -translate-y-1/2 border-t border-dashed"
              />
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
            <RegistryNode />
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
                  first={firstOutcome(
                    op.id,
                    checkDelay(i),
                    checkKeyframeName(i),
                  )}
                  second={secondOutcome(
                    checkDelay(i) + HALF,
                    checkKeyframeName(i),
                  )}
                />
              ))}
            </div>
          </div>
        </div>

        <div className="hidden flex-col gap-3 px-4 py-4 motion-reduce:flex">
          <div className="flex items-center gap-3">
            <RegistryNode />
          </div>
          <div className="border-cc-card-border overflow-hidden rounded-lg border">
            {CLIENT_OPS.map((op) => {
              const outcome = firstOutcome(op.id, 0, "");
              return (
                <StaticClientRow
                  key={op.id}
                  label={op.label}
                  tone={outcome.tone}
                  text={outcome.text}
                />
              );
            })}
          </div>
          <div className="flex flex-col gap-3 @[480px]:flex-row">
            <StaticVerdictCard
              version={V14.version}
              diff={V14.diff}
              diffTone="danger"
              verdict={V14.verdict}
              verdictTone="danger"
              schemaFile={SCHEMA_FILE}
            />
            <StaticVerdictCard
              version={V15.version}
              diff={V15.diff}
              diffTone="warning"
              verdict={V15.verdict}
              verdictTone="success"
              schemaFile={SCHEMA_FILE}
            />
          </div>
        </div>
      </div>
    </AppWindow>
  );
}
