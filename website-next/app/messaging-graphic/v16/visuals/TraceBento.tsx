"use client";

/**
 * TraceBento — two-tile bento for the "every hop is a span in Nitro" section.
 * Embeds the real Nitro chart primitives (TraceWaterfall, CountUp, Sparkline)
 * inside a NitroCanvas so their `--t-*` token vars resolve, proving the
 * cross-service messaging story with actual product UI, not an illustration.
 */
import type { CSSProperties, ReactNode } from "react";

import { CountUp, NitroTheme, Sparkline, TraceWaterfall } from "@/src/nitro";
import type { Trace } from "@/src/nitro/lib/data/types";

import { CORAL, GREEN } from "../palette";

/* ----------------------------------------------------------------------------
   NitroCanvas — wraps chart primitives so their `--t-*` token vars resolve;
   stays transparent so the card surface shows through (analytics-page idiom).
---------------------------------------------------------------------------- */

interface NitroCanvasProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly style?: CSSProperties;
}

function NitroCanvas({ children, className, style }: NitroCanvasProps) {
  return (
    <NitroTheme
      theme="dark"
      reducedMotion="user"
      className={className}
      style={{ background: "transparent", ...style }}
    >
      {children}
    </NitroTheme>
  );
}

/* ----------------------------------------------------------------------------
   Trace fixture — one order being placed, followed across the broker gap:
   the producing request, the rabbitmq delivery, and the consuming service all
   hang off a single trace id.
---------------------------------------------------------------------------- */

const TRACE: Trace = {
  totalMs: 210,
  spans: [
    {
      id: "s1",
      name: "POST /orders",
      kind: "server",
      startMs: 0,
      durationMs: 96,
      depth: 0,
    },
    {
      id: "s2",
      name: "PlaceOrder handler",
      kind: "internal",
      startMs: 6,
      durationMs: 52,
      depth: 1,
    },
    {
      id: "s3",
      name: "INSERT order + outbox",
      kind: "http",
      startMs: 14,
      durationMs: 26,
      depth: 2,
    },
    {
      id: "s4",
      name: "publish OrderPlaced",
      kind: "internal",
      startMs: 44,
      durationMs: 12,
      depth: 2,
    },
    {
      id: "s5",
      name: "rabbitmq deliver",
      kind: "http",
      startMs: 58,
      durationMs: 34,
      depth: 1,
    },
    {
      id: "s6",
      name: "consume OrderPlaced",
      kind: "server",
      startMs: 94,
      durationMs: 112,
      depth: 1,
    },
    {
      id: "s7",
      name: "CreateInvoice handler",
      kind: "internal",
      startMs: 102,
      durationMs: 74,
      depth: 2,
    },
    {
      id: "s8",
      name: "cache invalidate",
      kind: "internal",
      startMs: 180,
      durationMs: 22,
      depth: 2,
    },
  ],
};

const DELIVERY_LATENCY = [12, 14, 11, 18, 13, 22, 17, 14, 12, 15];

/* ----------------------------------------------------------------------------
   Tile scaffolding
---------------------------------------------------------------------------- */

interface TileHeaderProps {
  readonly index: string;
  readonly title: string;
  readonly hint: string;
}

function TileHeader({ index, title, hint }: TileHeaderProps) {
  return (
    <div className="flex items-baseline gap-3 px-5 pt-5">
      <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.14em]">
        {index}
      </span>
      <h3 className="text-cc-heading font-heading text-h6">{title}</h3>
      <span className="text-cc-nav-label ml-auto text-right font-mono text-[0.6rem] tracking-[0.16em] uppercase">
        {hint}
      </span>
    </div>
  );
}

interface TileProps {
  readonly className?: string;
  readonly children: ReactNode;
}

function Tile({ className, children }: TileProps) {
  return (
    <div
      className={[
        "border-cc-card-border bg-cc-card-bg relative flex flex-col overflow-hidden rounded-2xl border backdrop-blur",
        className ?? "",
      ].join(" ")}
    >
      {children}
    </div>
  );
}

/* ----------------------------------------------------------------------------
   Component
---------------------------------------------------------------------------- */

export function TraceBento() {
  return (
    <div className="grid w-full grid-cols-1 gap-4 sm:grid-cols-6" aria-hidden>
      {/* Tile A — the full waterfall: producer, broker hop, consumer, one trace. */}
      <Tile className="sm:col-span-4">
        <TileHeader
          index="a"
          title="One message, end to end"
          hint="TRACE · 7f3a·9b2e"
        />
        <div className="px-5 pt-4 pb-3">
          <NitroCanvas>
            <TraceWaterfall trace={TRACE} />
          </NitroCanvas>
        </div>
      </Tile>

      {/* Tile B — the numbers that prove the correlation survived the broker. */}
      <Tile className="sm:col-span-2">
        <TileHeader index="b" title="Correlated across the gap" hint="OTEL" />
        <div className="flex flex-1 flex-col justify-evenly gap-6 px-5 pt-5 pb-5">
          <div>
            <NitroCanvas className="h-9">
              <CountUp
                value={210}
                format={(n) => `${Math.round(n)} ms`}
                style={{ justifyContent: "flex-start", fontSize: 28 }}
              />
            </NitroCanvas>
            <p className="text-cc-nav-label mt-1.5 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
              {"publish -> consume, end to end"}
            </p>
          </div>

          <div>
            <NitroCanvas className="h-10">
              <Sparkline values={DELIVERY_LATENCY} stroke={CORAL} />
            </NitroCanvas>
            <p className="text-cc-nav-label mt-2 font-mono text-[0.6rem] tracking-[0.14em] uppercase">
              delivery latency
            </p>
          </div>

          <div>
            <span
              className="inline-flex items-center gap-2 rounded-full border px-2.5 py-1 font-mono text-[0.65rem] tracking-[0.1em] uppercase"
              style={{
                color: GREEN,
                borderColor: `${GREEN}44`,
                backgroundColor: `${GREEN}14`,
              }}
            >
              <span
                className="h-1.5 w-1.5 shrink-0 rounded-full"
                style={{
                  backgroundColor: GREEN,
                  boxShadow: `0 0 8px ${GREEN}aa`,
                }}
              />
              Correlation propagated
            </span>
          </div>
        </div>
      </Tile>
    </div>
  );
}
