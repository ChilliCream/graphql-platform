import type { ComponentPropsWithoutRef } from "react";

import { AppWindow } from "@/src/components/AppWindow";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { CheckGlyph } from "@/src/icons/CheckGlyph";

/**
 * The `performance-by-design` panel: an AppWindow tracing one request's
 * execution top to bottom — a precomputed plan, a plan-cache hit,
 * deduplicated downstream fetches, then incremental delivery via
 * `@defer`/`@stream`. Rows reveal on scroll with a staggered delay; the rest
 * frame (every row settled) is always the final state, so `motion-reduce`
 * needs nothing special beyond what `RevealOnScroll` already honours. Below
 * 500px of the window's own width the timing value drops under the label
 * instead of sharing its row, via a container query scoped to this window.
 */

type Tone = "success" | "warning";

const TONE_TEXT: Record<Tone, string> = {
  success: "text-cc-success",
  warning: "text-cc-warning",
};

/** Half-filled ring for a row still arriving, unlike CheckGlyph's completed mark. */
function StreamGlyph(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg viewBox="0 0 16 16" fill="none" aria-hidden="true" {...props}>
      <circle
        cx="8"
        cy="8"
        r="6"
        stroke="currentColor"
        strokeWidth={2}
        strokeOpacity={0.35}
      />
      <path
        d="M8 2a6 6 0 0 1 6 6"
        stroke="currentColor"
        strokeWidth={2}
        strokeLinecap="round"
      />
    </svg>
  );
}

interface ExecutionRowSpec {
  readonly tone: Tone;
  readonly label: string;
  readonly detail: string;
}

const ROWS: readonly ExecutionRowSpec[] = [
  {
    tone: "success",
    label: "operation plan",
    detail: "precomputed at build → loaded",
  },
  { tone: "success", label: "plan cache", detail: "hit · 0 ms planning" },
  {
    tone: "success",
    label: "downstream fetches",
    detail: "5 requested → 3 after deduplication",
  },
  { tone: "success", label: "initial payload", detail: "sent · 12 ms" },
  { tone: "warning", label: "@defer · reviews", detail: "+38 ms" },
  {
    tone: "warning",
    label: "@stream · orders",
    detail: "3 of 20 streaming · +64 ms",
  },
];

const ROW_DELAYS = [
  "",
  "delay-75",
  "delay-150",
  "delay-200",
  "delay-300",
  "delay-500",
];

const ROW_CLASS = "border-cc-card-border border-b px-4 py-3 last:border-b-0";
const ROW_STACK_CLASS =
  "flex min-w-0 flex-1 flex-col gap-0.5 @min-[500px]:flex-row @min-[500px]:items-baseline @min-[500px]:justify-between @min-[500px]:gap-3";

interface ExecutionRowProps extends ExecutionRowSpec {
  readonly delayClassName: string;
}

function ExecutionRow({
  tone,
  label,
  detail,
  delayClassName,
}: ExecutionRowProps) {
  const Glyph = tone === "success" ? CheckGlyph : StreamGlyph;
  return (
    <RevealOnScroll className={`${ROW_CLASS} ${delayClassName}`}>
      <div className="flex items-start gap-3">
        <span
          className={`mt-0.5 flex h-4 w-4 shrink-0 items-center justify-center ${TONE_TEXT[tone]}`}
        >
          <Glyph width={12} height={12} />
        </span>
        <div className={ROW_STACK_CLASS}>
          <span className="text-cc-heading font-mono text-[0.7rem]">
            {label}
          </span>
          <span
            className={`font-mono text-[0.7rem] ${TONE_TEXT[tone]} @min-[500px]:text-right`}
          >
            {detail}
          </span>
        </div>
      </div>
    </RevealOnScroll>
  );
}

export function PerformanceWindow() {
  return (
    <AppWindow
      title={
        <span className="text-cc-prose">query GetOrderSummary · execution</span>
      }
      footer={
        <div className="flex flex-wrap items-center justify-between gap-2">
          <span className="text-cc-ink-dim font-mono text-[0.7rem]">
            no runtime planning
          </span>
          <span className="text-cc-success font-mono text-[0.7rem]">
            ✓ first bytes in 12 ms
          </span>
        </div>
      }
    >
      <div className="@container">
        {ROWS.map((row, i) => (
          <ExecutionRow
            key={row.label}
            {...row}
            delayClassName={ROW_DELAYS[i]}
          />
        ))}
      </div>
    </AppWindow>
  );
}
