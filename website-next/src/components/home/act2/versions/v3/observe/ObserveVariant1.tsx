/**
 * "Production view" scene, concept 1 ("Operation caught mid-spike"), v3
 * "Signal & Metrics" (dark cc-* panel).
 *
 * Re-expresses the v1 Nitro operation-detail tile (the `checkout` operation
 * caught the instant its p99 spikes) in v3's locked metrics strategy with layout
 * archetype A (stat-left / signal-right): the measurable result leads. The hero
 * is a single large cream numeral - p99 842 ms - over a lowercase mono caption,
 * sitting left. Bound directly to its right, the one teal accent is a thin 1px
 * p99 sparkline that stays flat near baseline then kinks sharply up across a
 * dashed grey SLO line, carrying exactly one filled teal node on the spike apex
 * (the value the headline names). The single status hue is the coral "breach"
 * chip in the eyebrow: the p99 has crossed its SLO, a genuine state. A
 * dashed-divider footer prints the one-line reading.
 *
 * Content coheres with the v3 checkout incident set (see ObserveVariant3, same
 * operation: p95 812 ms, 21% 5xx, firing): operation `checkout` / p99, a series
 * flat near 320 ms then climbing to an 842 ms apex past a 500 ms SLO.
 *
 * Static settled frame: server component, no "use client", no hooks, no motion.
 * Every SVG id is prefixed "v3-observe-1-". Local cc-* dark palette mirrors the
 * cc-* tokens; teal is the only decorative hue and is bound to the p99 spike,
 * coral is rationed to the one real-status chip (the SLO breach).
 */

interface ObserveVariant1Props {
  readonly className?: string;
}

/* v3 "Signal & Metrics" strict cc-* dark palette. Teal is the ONLY decorative
 * hue and is bound to the hero metric (the p99 sparkline + its one node). Status
 * hues encode real state only: coral marks the SLO breach. */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
} as const;

const HEADING = '"Josefin Sans", Futura, sans-serif';

// p99 latency by recent window (ms): flat near 320 ms, then a sharp kink up to
// the 842 ms apex on-call is investigating. The 500 ms SLO sits between.
const P99_SERIES = [318, 322, 312, 320, 345, 560, 842] as const;
const SLO_MS = 500;

// Sparkline plot geometry (user units). Uniform-scaled (no preserveAspectRatio
// override) so the single teal node stays round at any width.
const SPARK_W = 150;
const SPARK_H = 54;
const SPARK_PAD_T = 6;
const SPARK_PAD_B = 6;

function buildSpark(values: readonly number[]) {
  const n = values.length;
  const min = Math.min(...values);
  const max = Math.max(...values);
  const top = SPARK_PAD_T;
  const bottom = SPARK_H - SPARK_PAD_B;
  const xOf = (i: number) => (i / (n - 1)) * SPARK_W;
  const yOf = (v: number) =>
    max === min
      ? (top + bottom) / 2
      : bottom - ((v - min) / (max - min)) * (bottom - top);

  const pts = values.map((v, i) => [xOf(i), yOf(v)] as const);
  const line = pts
    .map(([x, y], i) => `${i === 0 ? "M" : "L"}${x} ${y}`)
    .join(" ");
  return { line, apex: pts[pts.length - 1], sloY: yOf(SLO_MS) };
}

export function ObserveVariant1({ className }: ObserveVariant1Props) {
  const { line, apex, sloY } = buildSpark(P99_SERIES);

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow row: operation + the one real-status chip (coral breach) */}
        <div className="flex items-center justify-between gap-2">
          <span
            className="font-mono text-[0.58rem] tracking-[0.15em] uppercase"
            style={{ color: cc.navLabel }}
          >
            checkout / p99
          </span>
          <span
            className="inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 font-mono text-[0.5rem] tracking-[0.08em] uppercase"
            style={{ borderColor: `${cc.coral}59`, color: cc.coral }}
          >
            <span
              className="inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: cc.coral }}
            />
            breach
          </span>
        </div>

        {/* hero band (archetype A): cream numeral left, teal signal right */}
        <div className="mt-4 flex items-center gap-4">
          {/* the measurable result: p99 842 ms, the brightest thing in the cell */}
          <div className="shrink-0">
            <p className="flex items-baseline gap-1 leading-none">
              <span
                style={{
                  fontFamily: HEADING,
                  fontSize: "2.75rem",
                  fontWeight: 600,
                  lineHeight: 1,
                  color: cc.heading,
                  fontVariantNumeric: "tabular-nums",
                }}
              >
                842
              </span>
              <span
                className="font-mono text-[0.95rem]"
                style={{ color: cc.navLabel }}
              >
                ms
              </span>
            </p>
            <p
              className="mt-1.5 font-mono text-[0.7rem] lowercase"
              style={{ color: cc.inkDim }}
            >
              p99 mid-spike
            </p>
          </div>

          {/* the one teal signal: p99 kinking past the dashed SLO line, one node */}
          <div className="min-w-0 flex-1">
            <p
              className="text-right font-mono text-[0.5rem] tracking-[0.08em] uppercase"
              style={{ color: cc.navLabel }}
            >
              slo {SLO_MS}ms
            </p>
            <svg
              className="mt-1 block w-full"
              viewBox={`0 0 ${SPARK_W} ${SPARK_H}`}
              width="100%"
              role="img"
              aria-label="p99 latency sparkline, flat near 320 ms then climbing sharply past the SLO line to an 842 ms spike"
              style={{ overflow: "visible" }}
            >
              {/* dashed SLO threshold the series breaches (grey comparison) */}
              <line
                x1={0}
                y1={sloY}
                x2={SPARK_W}
                y2={sloY}
                stroke={cc.inkFaint}
                strokeWidth={1}
                strokeDasharray="4 4"
                vectorEffect="non-scaling-stroke"
              />
              {/* p99 polyline: single 1px teal stroke, no fill */}
              <path
                d={line}
                fill="none"
                stroke={cc.accent}
                strokeWidth={1}
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
              {/* the measure mark: surface ring + one filled teal node on the apex */}
              <circle cx={apex[0]} cy={apex[1]} r={3.5} fill={cc.surface} />
              <circle cx={apex[0]} cy={apex[1]} r={2.5} fill={cc.accent} />
            </svg>
          </div>
        </div>

        {/* interpretation caption under a dashed faint divider */}
        <div
          className="mt-4 border-t border-dashed pt-3"
          style={{ borderColor: cc.inkFaint }}
        >
          <p
            className="text-center font-mono text-[0.62rem] lowercase"
            style={{ color: cc.inkDim }}
          >
            one operation&apos;s health at a glance
          </p>
        </div>
      </div>
    </div>
  );
}
