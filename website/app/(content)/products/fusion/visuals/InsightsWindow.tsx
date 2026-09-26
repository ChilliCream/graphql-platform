import { AppWindow } from "@/src/components/AppWindow";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { CheckGlyph } from "@/src/icons/CheckGlyph";

type Tone = "muted" | "warning";

interface WaterfallSpan {
  readonly name: string;
  readonly startMs: number;
  readonly durationMs: number;
  readonly tone: Tone;
}

const TOTAL_MS = 150;

const SPANS: readonly WaterfallSpan[] = [
  { name: "POST /graphql", startMs: 0, durationMs: 150, tone: "muted" },
  {
    name: "query GetOrderSummary",
    startMs: 3,
    durationMs: 145,
    tone: "muted",
  },
  { name: "catalog · GetProducts", startMs: 9, durationMs: 26, tone: "muted" },
  {
    name: "orders · GetOrderHistory",
    startMs: 9,
    durationMs: 33,
    tone: "muted",
  },
  {
    name: "billing · GetInvoice",
    startMs: 44,
    durationMs: 92,
    tone: "warning",
  },
];

const AXIS_TICKS_MS = [0, 50, 100, 150] as const;

const ROW_DELAYS = ["", "delay-75", "delay-150", "delay-200", "delay-300"];

const TONE_TEXT_CLASS: Record<Tone, string> = {
  muted: "text-cc-ink-dim",
  warning: "text-cc-warning",
};
const TONE_BAR_CLASS: Record<Tone, string> = {
  muted: "bg-cc-nav-text/60",
  warning: "bg-cc-warning",
};

const ROW_CLASS = "border-cc-card-border border-b px-4 py-3 last:border-b-0";
const ROW_HEAD_CLASS =
  "flex min-w-0 flex-col gap-0.5 @min-[500px]:flex-row @min-[500px]:items-baseline @min-[500px]:justify-between @min-[500px]:gap-3";
const FOOTER_ROW_CLASS =
  "flex min-w-0 flex-col gap-0.5 @min-[500px]:flex-row @min-[500px]:items-baseline @min-[500px]:justify-between @min-[500px]:gap-3";

/** Axis ticks sit in their own zero-padding box so left% lines up with the bar tracks below. */
function TimeAxis() {
  return (
    <div className="px-4 pt-3 pb-1">
      <div className="relative h-4">
        {AXIS_TICKS_MS.map((tick) => {
          const pct = (tick / TOTAL_MS) * 100;
          const transform =
            tick === 0
              ? "none"
              : tick === TOTAL_MS
                ? "translateX(-100%)"
                : "translateX(-50%)";
          return (
            <span
              key={tick}
              className="text-cc-ink-dim absolute font-mono text-[0.7rem] whitespace-nowrap"
              style={{ left: `${pct}%`, transform }}
            >
              {tick} ms
            </span>
          );
        })}
      </div>
    </div>
  );
}

interface WaterfallRowProps extends WaterfallSpan {
  readonly delayClassName: string;
}

function WaterfallRow({
  name,
  startMs,
  durationMs,
  tone,
  delayClassName,
}: WaterfallRowProps) {
  const left = (startMs / TOTAL_MS) * 100;
  const width = (durationMs / TOTAL_MS) * 100;
  return (
    <RevealOnScroll className={`${ROW_CLASS} ${delayClassName}`}>
      <div className={ROW_HEAD_CLASS}>
        <span className="text-cc-heading font-mono text-[0.7rem]">{name}</span>
        <span
          className={`font-mono text-[0.7rem] ${TONE_TEXT_CLASS[tone]} @min-[500px]:text-right`}
        >
          {durationMs} ms
        </span>
      </div>
      <div className="bg-cc-accent/15 relative mt-2 h-1.5 rounded-full">
        <div
          className={`absolute inset-y-0 rounded-full ${TONE_BAR_CLASS[tone]}`}
          style={{ left: `${left}%`, width: `${width}%` }}
        />
      </div>
    </RevealOnScroll>
  );
}

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
      <div
        className="@container"
        role="img"
        aria-label="Trace of query GetOrderSummary: gateway request, then the operation plan's fetches to catalog and orders in parallel, then billing, the slowest of the three."
      >
        <TimeAxis />
        {SPANS.map((span, i) => (
          <WaterfallRow
            key={span.name}
            {...span}
            delayClassName={ROW_DELAYS[i]}
          />
        ))}
      </div>
    </AppWindow>
  );
}
