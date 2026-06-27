interface ObserveVariant2Props {
  readonly className?: string;
}

/**
 * Production-view scene, variant 2 (v5 "Schematic Lines"): distributed trace
 * waterfall.
 *
 * One request's trace reduced to a monoline waterfall of offset thin bars on a
 * shared 0-94 ms axis. Each span is a 1px line placed by (start, duration):
 * the root checkout (0-94), then its children users-svc REST (8-18), billing
 * gRPC (18-81) and the db SELECT (81-94). The billing hop is the genuine
 * bottleneck, so its line and label carry the one coral status hue.
 *
 * The single teal thread is the critical route the headline names: it leaves a
 * hollow teal source ring at the request entry, runs along the root span and
 * elbows down at t=18 onto the start of the slow billing hop, landing as a
 * solid teal dot exactly where the long coral bar begins. Strip the teal and
 * the coral and the whole thing reads as a quiet grey timeline; the eye lands
 * on the handoff into the hop where the time actually goes.
 *
 * Borrowed content (exact, from the v1 / v2 siblings): checkout 0-94 ms (root),
 * users-svc 8-18 (REST), billing 18-81 (63 ms, gRPC, the bottleneck), db 81-94
 * (SELECT). React Server Component: no hooks, no client APIs, settled final
 * frame only. Every svg id is prefixed "v5-observe-2-".
 */

const C = {
  surface: "#0c1322",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-observe-2-";

// Shared time axis: [0, 94] ms maps to x in [AXIS_X0, AXIS_X1].
const TOTAL_MS = 94;
const AXIS_X0 = 72;
const AXIS_X1 = 264;

function x(ms: number): number {
  return AXIS_X0 + (ms / TOTAL_MS) * (AXIS_X1 - AXIS_X0);
}

interface TraceSpan {
  readonly label: string;
  readonly start: number;
  readonly end: number;
  readonly y: number;
  readonly indent: number;
  // bottleneck: the one genuinely-firing span, drawn coral.
  readonly bottleneck: boolean;
}

const SPANS: readonly TraceSpan[] = [
  { label: "checkout", start: 0, end: 94, y: 38, indent: 6, bottleneck: false },
  {
    label: "users-svc",
    start: 8,
    end: 18,
    y: 60,
    indent: 16,
    bottleneck: false,
  },
  { label: "billing", start: 18, end: 81, y: 82, indent: 16, bottleneck: true },
  { label: "db", start: 81, end: 94, y: 104, indent: 16, bottleneck: false },
] as const;

// The teal thread elbows down where users-svc ends and the slow hop begins.
const HANDOFF_X = x(18);
const BILLING_Y = 82;
const ROOT_Y = 38;

export function ObserveVariant2({ className }: ObserveVariant2Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          distributed trace
        </p>

        {/* Monoline span waterfall floating directly on the card. */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="Distributed trace waterfall for the checkout request: four nested spans over 94 ms, with the critical path threading into the slow billing gRPC hop drawn coral as the bottleneck."
          className="mt-4"
          style={{ display: "block" }}
        >
          <defs>
            <marker
              id={`${ID}head-teal`}
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
                stroke={C.accent}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
            </marker>
          </defs>

          {/* Registration ticks: the timeline baseline / scale rhyme. */}
          <g
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          >
            {[72, 96, 120, 144, 168, 192, 216, 240, 264].map((tx) => (
              <line key={tx} x1={tx} y1="120" x2={tx} y2="125" />
            ))}
          </g>

          {/* Grey span bars. The root carries grey only past the teal handoff;
              users-svc and db are short grey lines with small open start nodes. */}
          {SPANS.map((span) => {
            if (span.bottleneck) {
              return null;
            }
            const isRoot = span.start === 0;
            const lineStart = isRoot ? HANDOFF_X : x(span.start);
            return (
              <g key={span.label}>
                {!isRoot && (
                  <circle
                    cx={x(span.start)}
                    cy={span.y}
                    r="2.5"
                    fill={C.surface}
                    stroke={C.inkFaint}
                    strokeWidth="1"
                    vectorEffect="non-scaling-stroke"
                  />
                )}
                <line
                  x1={lineStart}
                  y1={span.y}
                  x2={x(span.end)}
                  y2={span.y}
                  stroke={C.inkFaint}
                  strokeWidth="1"
                  strokeLinecap="round"
                  vectorEffect="non-scaling-stroke"
                />
              </g>
            );
          })}

          {/* The coral bottleneck: the slow billing gRPC hop, the one status hue. */}
          <line
            x1={HANDOFF_X}
            y1={BILLING_Y}
            x2={x(81)}
            y2={BILLING_Y}
            stroke={C.coral}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* The teal thread: source ring -> along the root -> elbow down onto
              the start of the slow hop, terminating in a solid teal dot. */}
          <path
            d={`M${AXIS_X0} ${ROOT_Y} L${HANDOFF_X} ${ROOT_Y} L${HANDOFF_X} ${
              BILLING_Y - 6
            }`}
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}head-teal)`}
          />
          <circle
            cx={AXIS_X0}
            cy={ROOT_Y}
            r="6"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx={HANDOFF_X} cy={BILLING_Y} r="2.5" fill={C.accent} />

          {/* Sparse span labels: root, two children, and the coral bottleneck. */}
          {SPANS.map((span) => (
            <text
              key={span.label}
              x={span.indent}
              y={span.y + 3}
              fontFamily={C.mono}
              fontSize="8"
              fill={span.bottleneck ? C.coral : C.ink}
            >
              {span.label}
            </text>
          ))}
        </svg>

        {/* Single footer numeral: where most of the 94 ms actually went. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            63 ms
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            of 94 ms, spent in the billing hop
          </p>
        </div>
      </div>
    </div>
  );
}
