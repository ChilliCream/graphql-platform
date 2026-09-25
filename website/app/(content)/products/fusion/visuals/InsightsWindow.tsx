import { AppWindow } from "@/src/components/AppWindow";
import { NitroFrame } from "@/src/components/NitroFrame";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { CheckGlyph } from "@/src/icons/CheckGlyph";
import { TraceWaterfall } from "@/src/nitro";
import type { Trace } from "@/src/nitro/lib/data/types";

const OPERATION_TRACE: Trace = {
  totalMs: 150,
  spans: [
    {
      id: "s1",
      name: "POST /graphql",
      kind: "server",
      startMs: 0,
      durationMs: 150,
      depth: 0,
    },
    {
      id: "s2",
      name: "query GetOrderSummary",
      kind: "graphql",
      startMs: 3,
      durationMs: 145,
      depth: 1,
    },
    {
      id: "s3",
      name: "catalog · GetProducts",
      kind: "http",
      startMs: 9,
      durationMs: 26,
      depth: 2,
    },
    {
      id: "s4",
      name: "orders · GetOrderHistory",
      kind: "http",
      startMs: 9,
      durationMs: 33,
      depth: 2,
    },
    {
      id: "s5",
      name: "billing · GetInvoice",
      kind: "http",
      startMs: 44,
      durationMs: 92,
      depth: 2,
    },
  ],
};

const FOOTER_ROW_CLASS =
  "flex min-w-0 flex-col gap-0.5 @min-[500px]:flex-row @min-[500px]:items-baseline @min-[500px]:justify-between @min-[500px]:gap-3";

export function InsightsWindow() {
  return (
    <AppWindow
      title={
        <span className="text-cc-prose">trace · query GetOrderSummary</span>
      }
      footer={
        <div className="@container flex flex-col gap-1.5">
          <RevealOnScroll className={FOOTER_ROW_CLASS}>
            <span className="text-cc-ink-dim font-mono text-[0.7rem]">
              latency driver
            </span>
            <span className="text-cc-warning font-mono text-[0.7rem]">
              billing drives 61% of latency
            </span>
          </RevealOnScroll>
          <RevealOnScroll className="flex items-center gap-2 delay-150">
            <CheckGlyph className="text-cc-success h-3.5 w-3.5 shrink-0" />
            <span className="text-cc-success min-w-0 flex-1 font-mono text-[0.7rem]">
              cost 184 of 1,000 · rate limit ok · usage recorded in Nitro
            </span>
          </RevealOnScroll>
        </div>
      }
    >
      <div className="px-4 py-4" style={{ zoom: 1.25 }}>
        <NitroFrame reducedMotion="user">
          <TraceWaterfall
            trace={OPERATION_TRACE}
            rowHeight={30}
            durationMs={4200}
            once
            ariaLabel="Trace of query GetOrderSummary: gateway request, then the operation plan's fetches to catalog and orders in parallel, then billing, the slowest of the three."
          />
        </NitroFrame>
      </div>
    </AppWindow>
  );
}
