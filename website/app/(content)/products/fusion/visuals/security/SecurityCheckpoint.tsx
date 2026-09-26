"use client";

import { useRef } from "react";

import { AppWindow } from "@/src/components/AppWindow";
import {
  useCycle,
  useElementMotion,
} from "@/src/components/LayeredDiagram/hooks";
import { ShieldGlyph } from "@/src/icons/ShieldGlyph";

import {
  CHECKS,
  CLIENTS,
  EVENTS,
  PHASE_MS,
  PHASES_PER_EVENT,
  SERVICES,
  checkStatus,
  frameAt,
  wrap,
} from "./data";
import { AuditLine, CheckRow, NodeCard, Track } from "./parts";
import type { NodeTone } from "./parts";

/**
 * The Security row's graphic: an animated policy checkpoint at the router.
 * Requests travel in from clients, the router evaluates them against three
 * stacked checks, an allowed one continues to a service and a denied one
 * stops at the router with a red cross and "403" — never reaching a service
 * either way. Every decision appends a line to the audit trail strip below.
 */

const KEYFRAMES = `
@keyframes sec-pulse-row {
  0% { left: 2%; opacity: 0; }
  12% { opacity: 1; }
  88% { opacity: 1; }
  100% { left: 96%; opacity: 0; }
}
@keyframes sec-pulse-col {
  0% { top: 2%; opacity: 0; }
  12% { opacity: 1; }
  88% { opacity: 1; }
  100% { top: 96%; opacity: 0; }
}
`;

/** A rejected (not-safelisted) request carries no status code; a policy denial shows 403. */
const CHECK_CODE: readonly (string | undefined)[] = [undefined, "403", "403"];

interface CheckStackProps {
  readonly failAt: number | null;
  readonly show: boolean;
}

/** The router's three stacked checks, evaluated in order up to `failAt`. */
function CheckStack({ failAt, show }: CheckStackProps) {
  return (
    <div className="border-cc-card-border flex-1 rounded-lg border px-2.5 py-2">
      <div className="text-cc-ink-dim mb-1.5 flex items-center gap-1.5 font-mono text-[0.7rem] uppercase">
        <ShieldGlyph className="h-3.5 w-3.5" />
        Fusion router
      </div>
      <div className="flex flex-col gap-1">
        {CHECKS.map((check, i) => (
          <CheckRow
            key={check.key}
            label={check.label}
            status={show ? checkStatus(failAt, i) : "idle"}
            code={CHECK_CODE[i]}
          />
        ))}
      </div>
      {show && failAt !== null && (
        <div className="text-cc-danger mt-1 text-center font-mono text-[0.7rem]">
          {failAt === 0 ? "rejected · not in safelist" : "denied · 403"}
        </div>
      )}
    </div>
  );
}

/** A complete, motionless frame: one request already through, one stopped at the router. */
function StaticCheckpoint() {
  const allowed = EVENTS[0];
  const denied = EVENTS[2];

  return (
    <div className="flex flex-col gap-3 @min-[480px]:flex-row @min-[480px]:items-stretch">
      <div className="grid grid-cols-2 gap-1.5 @min-[480px]:w-28 @min-[480px]:grid-cols-1">
        {CLIENTS.map((client, i) => (
          <NodeCard
            key={client.key}
            label={client.label}
            lit={i === allowed.client || i === denied.client}
            tone={i === denied.client ? "danger" : "success"}
          />
        ))}
      </div>

      <Track active={false} tone="success" stepKey="static-in" />
      <CheckStack failAt={denied.failAt} show />
      <Track active={false} tone="success" stepKey="static-out" />

      <div className="grid grid-cols-3 gap-1.5 @min-[480px]:w-28 @min-[480px]:grid-cols-1">
        {SERVICES.map((service, i) => (
          <NodeCard
            key={service.key}
            label={service.label}
            lit={i === allowed.service}
            tone="success"
          />
        ))}
      </div>
    </div>
  );
}

interface AnimatedCheckpointProps {
  readonly step: number;
}

/** One event's four phases: arrive, check, resolve, settle. */
function AnimatedCheckpoint({ step }: AnimatedCheckpointProps) {
  const { event, phase } = frameAt(step);
  const arriving = phase === 0;
  const resolving = phase === 2;
  const settled = phase === 3;
  const allowed = event.service !== null;

  const clientTone: NodeTone =
    resolving || settled ? (allowed ? "success" : "danger") : "active";

  return (
    <div className="flex flex-col gap-3 @min-[480px]:flex-row @min-[480px]:items-stretch">
      <div className="grid grid-cols-2 gap-1.5 @min-[480px]:w-28 @min-[480px]:grid-cols-1">
        {CLIENTS.map((client, i) => (
          <NodeCard
            key={client.key}
            label={client.label}
            lit={i === event.client && !settled}
            tone={clientTone}
          />
        ))}
      </div>

      <Track active={arriving} tone="active" stepKey={`in-${step}`} />
      <CheckStack failAt={event.failAt} show={phase === 1 || resolving} />
      <Track
        active={resolving && allowed}
        tone="success"
        stepKey={`out-${step}`}
      />

      <div className="grid grid-cols-3 gap-1.5 @min-[480px]:w-28 @min-[480px]:grid-cols-1">
        {SERVICES.map((service, i) => (
          <NodeCard
            key={service.key}
            label={service.label}
            lit={(resolving || settled) && i === event.service}
            tone="success"
          />
        ))}
      </div>
    </div>
  );
}

function AuditStrip({ step }: { readonly step: number }) {
  const { eventIndex, phase } = frameAt(step);
  const currentIndex = wrap(
    phase >= 2 ? eventIndex : eventIndex - 1,
    EVENTS.length,
  );
  const previousIndex = wrap(currentIndex - 1, EVENTS.length);
  const current = EVENTS[currentIndex];
  const previous = EVENTS[previousIndex];

  return (
    <div className="flex flex-col gap-1">
      <AuditLine
        text={previous.auditLine}
        tone={previous.service !== null ? "success" : "danger"}
        dim
      />
      <AuditLine
        text={current.auditLine}
        tone={current.service !== null ? "success" : "danger"}
      />
    </div>
  );
}

function StaticAuditStrip() {
  return (
    <div className="flex flex-col gap-1">
      <AuditLine text={EVENTS[0].auditLine} tone="success" />
      <AuditLine text={EVENTS[2].auditLine} tone="danger" />
    </div>
  );
}

export function SecurityCheckpoint() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const steps = EVENTS.length * PHASES_PER_EVENT;
  const step = useCycle(running, steps, PHASE_MS, 2);

  return (
    <AppWindow
      title={
        <span className="text-cc-prose">
          fusion gateway · policy checkpoint
        </span>
      }
      footer={
        <div className="flex flex-col gap-1">
          <span className="text-cc-ink-dim font-mono text-[0.7rem] uppercase">
            audit trail · Nitro
          </span>
          {running ? <AuditStrip step={step} /> : <StaticAuditStrip />}
        </div>
      }
    >
      <div
        ref={ref}
        className="@container px-3 py-3 sm:px-4 sm:py-4"
        role="img"
        aria-label="Requests from Web, Mobile, Partner and an unrecognized client arrive at the Fusion router, which checks each against a safelist, OPA and a custom policy. Allowed requests continue to Catalog, Orders or Billing; a denied request stops at the router with a 403 and a non-safelisted operation is rejected at the first check. Neither reaches a service."
      >
        <style>{KEYFRAMES}</style>
        {running ? <AnimatedCheckpoint step={step} /> : <StaticCheckpoint />}
      </div>
    </AppWindow>
  );
}
