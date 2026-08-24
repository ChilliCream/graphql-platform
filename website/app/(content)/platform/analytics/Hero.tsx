import { ButtonRow } from "@/src/components/ButtonRow";
import { MockWindowChrome } from "@/src/components/MockWindowChrome";
import { NitroFrame } from "@/src/components/NitroFrame";
import { PatternBand } from "@/src/components/PatternBand";
import { HEALTH_COLOR, StatusDot } from "@/src/components/StatusDot";
import type { HealthStatus } from "@/src/components/StatusDot";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { AreaChart } from "@/src/icons/AreaChart";
import { TraceWaterfall } from "@/src/nitro";
import type { Trace } from "@/src/nitro/lib/data/types";

import { CORAL, TEAL } from "./palette";

interface StatusBadgeProps {
  readonly status: HealthStatus;
  readonly label: string;
}

function StatusBadge({ status, label }: StatusBadgeProps) {
  const color = HEALTH_COLOR[status];
  return (
    <span
      className="inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.6rem] tracking-[0.1em] uppercase"
      style={{
        color,
        borderColor: `color-mix(in srgb, ${color} 33.33%, transparent)`,
        backgroundColor: `color-mix(in srgb, ${color} 7.84%, transparent)`,
      }}
    >
      <StatusDot status={status} pulse={status !== "healthy"} />
      {label}
    </span>
  );
}

interface LegendDotProps {
  readonly status: HealthStatus;
  readonly label: string;
}

function LegendDot({ status, label }: LegendDotProps) {
  return (
    <span className="flex items-center gap-2">
      <StatusDot status={status} pulse={status !== "healthy"} />
      <span className="text-cc-ink-dim font-mono text-[0.66rem] tracking-wide">{label}</span>
    </span>
  );
}

const TRACE_ID = "7f3a·9b2e·c1";

const CHECKOUT_TRACE: Trace = {
  totalMs: 318,
  spans: [
    {
      id: "s1",
      name: "POST /graphql",
      kind: "server",
      startMs: 0,
      durationMs: 318,
      depth: 0,
    },
    {
      id: "s2",
      name: "mutation checkout",
      kind: "graphql",
      startMs: 4,
      durationMs: 306,
      depth: 1,
    },
    {
      id: "s3",
      name: "users-svc · GET /me",
      kind: "http",
      startMs: 16,
      durationMs: 44,
      depth: 2,
    },
    {
      id: "s4",
      name: "billing · Charge",
      kind: "http",
      startMs: 67,
      durationMs: 204,
      depth: 2,
    },
    {
      id: "s5",
      name: "worker · receipt.enqueue",
      kind: "internal",
      startMs: 159,
      durationMs: 58,
      depth: 2,
    },
    {
      id: "s6",
      name: "orders.db · INSERT",
      kind: "internal",
      startMs: 271,
      durationMs: 38,
      depth: 2,
    },
  ],
};

const SPIKE_POINTS = [18, 21, 19, 24, 22, 26, 23, 28, 31, 27, 34, 30, 41, 52, 71, 96, 102, 88];

interface MiniMetricProps {
  readonly label: string;
  readonly value: string;
  readonly tone?: string;
}

function MiniMetric({ label, value, tone }: MiniMetricProps) {
  return (
    <div>
      <p className="text-cc-ink-dim font-mono text-[0.56rem] tracking-[0.1em] uppercase">{label}</p>
      <p
        className="text-cc-heading mt-0.5 font-mono text-sm font-semibold tabular-nums"
        style={tone ? { color: tone } : undefined}
      >
        {value}
      </p>
    </div>
  );
}

function IncidentArtifact() {
  return (
    <div className="relative">
      <div
        className="pointer-events-none absolute -inset-6 -z-10 rounded-[2.5rem] opacity-70 blur-3xl"
        style={{
          background:
            "radial-gradient(55% 55% at 60% 25%, rgba(94,234,212,0.22), transparent 70%), radial-gradient(50% 50% at 30% 90%, rgba(240,120,106,0.16), transparent 70%)",
        }}
        aria-hidden
      />

      <MockWindowChrome
        className="z-20 mx-auto max-w-md"
        header={{
          variant: "custom",
          content: (
            <span className="text-cc-ink-dim font-mono text-[0.62rem] tracking-[0.12em] uppercase">
              operation · checkout
            </span>
          ),
        }}
        headerRight={<StatusBadge status="warning" label="Warning" />}
        headerClassName="flex items-center justify-between px-5 py-2.5"
        shadow="none"
        rounded="rounded-2xl"
        surfaceClassName="bg-cc-surface/95 shadow-[0_30px_70px_-30px_rgba(0,0,0,0.8)] backdrop-blur"
      >
        <div className="p-5">
          <div className="flex items-start justify-between">
            <div>
              <p className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.12em] uppercase">operation</p>
              <p className="text-cc-heading mt-0.5 font-mono text-sm">mutation checkout</p>
            </div>
            <div className="text-right">
              <p className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.12em] uppercase">p99</p>
              <p className="mt-0.5 font-mono text-lg font-semibold tabular-nums" style={{ color: CORAL }}>
                318ms
                <span className="ml-1 align-middle text-[0.58rem] font-normal">▲ 7.6×</span>
              </p>
            </div>
          </div>
          <div className="relative mt-4">
            <AreaChart points={SPIKE_POINTS} stroke={CORAL} fill={CORAL} id="hero-spike" />
            <span className="text-cc-ink-dim absolute top-0 left-0 font-mono text-[0.56rem]">latency / 5m</span>
          </div>
          <div className="border-cc-card-border mt-4 grid grid-cols-3 gap-3 border-t pt-3">
            <MiniMetric label="p95" value="42ms" />
            <MiniMetric label="throughput" value="1.2k/m" />
            <MiniMetric label="errors" value="0.3%" tone="var(--color-cc-warning)" />
          </div>
        </div>
      </MockWindowChrome>

      <MockWindowChrome
        className="z-0 mt-4"
        header={{
          variant: "custom",
          content: (
            <span className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.12em] uppercase">
              distributed trace · checkout
            </span>
          ),
        }}
        headerRight={<span className="text-cc-ink-dim font-mono text-[0.6rem] tabular-nums">{TRACE_ID} · 318ms</span>}
        headerClassName="flex items-center justify-between px-5 py-2.5"
        footer={
          <>
            <StatusDot status="error" />
            <span className="text-cc-ink-dim font-mono text-[0.6rem]">
              <span style={{ color: CORAL }}>204ms</span> of this 318ms request were spent in the billing service.
            </span>
          </>
        }
        footerClassName="bg-cc-surface/40 flex items-center gap-2 px-5 py-2.5"
        shadow="none"
        rounded="rounded-2xl"
        surfaceClassName="bg-cc-card-bg shadow-[0_30px_70px_-34px_rgba(0,0,0,0.8)] backdrop-blur"
      >
        <div className="px-5 pt-4 pb-2">
          <NitroFrame>
            <TraceWaterfall trace={CHECKOUT_TRACE} rowHeight={30} durationMs={4500} once />
          </NitroFrame>
        </div>
      </MockWindowChrome>
    </div>
  );
}

export function Hero() {
  return (
    <PatternBand pattern="grid" flush className="border-b py-16 sm:py-24">
      <div className="grid items-center gap-12 lg:grid-cols-[0.95fr_1.05fr]">
        <div>
          <h1 className="font-heading text-h2 text-cc-heading sm:text-h1 tracking-tight">
            See what the <span style={{ color: TEAL }}>API</span> is doing.
          </h1>
          <p className="text-cc-prose font-body text-lead mt-6 max-w-xl font-normal">
            Track latency, errors, and throughput for the operations your services report. When something slows down,
            open the related traces and inspect which calls took the time.
          </p>
          <ButtonRow align="start" className="mt-9">
            <SolidButton href="https://nitro.chillicream.com">Start for Free</SolidButton>
            <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">Read the Docs</OutlineButton>
          </ButtonRow>

          <div className="mt-8 flex flex-wrap items-center gap-x-5 gap-y-2">
            <LegendDot status="healthy" label="healthy" />
            <LegendDot status="warning" label="warning" />
            <LegendDot status="error" label="error" />
          </div>
        </div>

        <IncidentArtifact />
      </div>
    </PatternBand>
  );
}
