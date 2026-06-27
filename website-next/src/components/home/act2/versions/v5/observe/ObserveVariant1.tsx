interface ObserveVariant1Props {
  readonly className?: string;
}

/**
 * "Production view" scene, v5 "Schematic Lines", concept #1: operation caught
 * mid-spike.
 *
 * A reductive monoline sparkline of the `checkout` operation's p99 latency. The
 * grey skeleton is a horizontal dashed SLO threshold (60ms) plus a strip of
 * registration ticks acting as the time baseline. The single teal thread is the
 * p99 trace: it leaves a hollow teal source ring on the left, runs flat along the
 * healthy baseline, and stays teal up to the moment it breaches the SLO line. At
 * the breach the thread turns coral (the one real status: firing) and spikes to
 * an open coral focal ring with a solid coral dot, the caught spike at 86ms.
 *
 * Content matches the v1 / v2 siblings: operation `checkout`, span-kind `query`,
 * p99 climbing to 86ms past a 60ms SLO, p95 42ms. Exactly one teal accent (the
 * healthy trace + source ring) and one status hue (coral, the firing span);
 * everything else stays cc-ink-faint grey.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only.
 * Every svg id is prefixed "v5-observe-1-".
 */

const C = {
  ink: "#a1a3af",
  navLabel: "#62748e",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  accent: "#5eead4",
  firing: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-observe-1-";

// p99 trace, healthy span: flat noisy baseline up to the SLO breach point.
const HEALTHY_PATH =
  "M30 96 L46.9 93.2 L63.8 94.6 L80.8 91.8 L97.7 93.2 L114.6 90.3 " +
  "L131.5 91.8 L148.5 88.9 L165.4 90.3 L182.3 87.5 L199.2 81.8 L216.2 69.1 L219.7 65";

// p99 trace, firing span: from the SLO breach up to the caught spike.
const FIRING_PATH = "M219.7 65 L233.1 49.3 L246.3 32.7";

export function ObserveVariant1({ className }: ObserveVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          operation health
        </p>

        {/* p99 sparkline: teal trace turns coral where it breaches the SLO line */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="checkout p99 latency flat near 40ms then spiking past a 60ms SLO to 86ms"
          className="mt-4"
          style={{ display: "block", overflow: "visible", fontFamily: C.mono }}
        >
          <defs>
            <marker
              id={`${ID}spike`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.5 L5 3 L0 5.5"
                fill="none"
                stroke={C.firing}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>
          </defs>

          {/* dashed SLO threshold (boundary), drawn behind the trace */}
          <line
            x1="24"
            y1="65"
            x2="256"
            y2="65"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeDasharray="2 3"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
          />

          {/* registration ticks: the time baseline / scale */}
          {Array.from({ length: 13 }, (_, i) => {
            const x = 30 + (i * 220) / 12;
            return (
              <line
                key={`${ID}tick-${i}`}
                x1={x}
                y1="112"
                x2={x}
                y2="117"
                stroke={C.inkFaint}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
                strokeLinecap="round"
              />
            );
          })}

          {/* teal thread: the healthy p99 trace */}
          <path
            d={HEALTHY_PATH}
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* firing span (coral status): the spike past the SLO */}
          <path
            d={FIRING_PATH}
            fill="none"
            stroke={C.firing}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}spike)`}
          />

          {/* hollow teal source ring: the operation the trace belongs to */}
          <circle
            cx="24"
            cy="96"
            r="5"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* caught spike: open coral focal ring + solid coral dot */}
          <circle
            cx="250"
            cy="28"
            r="6"
            fill="none"
            stroke={C.firing}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="250" cy="28" r="2.5" fill={C.firing} />

          {/* sparse micro-labels */}
          <text x="24" y="24" fontSize="9" fill={C.ink}>
            checkout
          </text>
          <text
            x="24"
            y="35"
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.navLabel}
          >
            QUERY &middot; P99
          </text>
          <text
            x="24"
            y="61"
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.navLabel}
          >
            SLO 60MS
          </text>
          <text x="240" y="26" textAnchor="end" fontSize="9" fill={C.firing}>
            86ms
          </text>
        </svg>

        {/* single footer numeral: the other headline metric, holding under SLO */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            42ms
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            p95 latency, within slo
          </p>
        </div>
      </div>
    </div>
  );
}
