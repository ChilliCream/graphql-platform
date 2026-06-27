interface ObserveVariant5Props {
  readonly className?: string;
}

/**
 * "Production view" scene, v5 "Schematic Lines", concept #5: nitro trace replay.
 *
 * The resolved span tree of `nitro trace 4b1c8f2a` reduced to a monoline replay
 * scrubber. A single grey track carries the four resolved hops as evenly-spaced
 * frame nodes (users-svc, catalog, billing, orders); a strip of evenly-spaced
 * frame ticks below is the replay scale. The billing gRPC hop is the genuine
 * bottleneck, so its node and labels carry the one coral status hue.
 *
 * The single teal thread is the replay route the headline names: it leaves a
 * hollow teal source ring at the trace origin, runs along the played portion of
 * the track, and parks the playhead (a teal stem to a solid teal dot handle)
 * exactly on the slow billing hop. Strip the teal and the coral and the whole
 * thing reads as a quiet grey scrubber; the eye lands on the frame where the
 * time actually went.
 *
 * Borrowed content (exact, from the sibling variants): trace id 4b1c8f2a, the
 * resolved hops users-svc / catalog / billing (gRPC) / orders, billing the slow
 * hop at 214 ms, 93% of the 231 ms request. React Server Component: no hooks, no
 * client APIs, settled final frame only. Every svg id is prefixed
 * "v5-observe-5-".
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

const ID = "v5-observe-5-";

// The track and where the playhead is parked.
const RAIL_Y = 72;
const RAIL_X0 = 40;
const RAIL_X1 = 256;
const SOURCE_X = 28;
const PLAYHEAD_X = 182;
const HANDLE_Y = 48;

interface Hop {
  readonly label: string;
  readonly x: number;
  // slow: the one genuinely-degraded hop, drawn coral.
  readonly slow?: boolean;
}

// The four resolved child hops, evenly spaced as replay frames on the track.
const HOPS: readonly Hop[] = [
  { label: "users-svc", x: 78 },
  { label: "catalog", x: 130 },
  { label: "billing", x: PLAYHEAD_X, slow: true },
  { label: "orders", x: 234 },
] as const;

export function ObserveVariant5({ className }: ObserveVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          trace replay
        </p>

        {/* Monoline replay scrubber floating directly on the card. */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="Replay scrubber for nitro trace 4b1c8f2a: four resolved hops on one track, the playhead parked on the slow billing gRPC hop drawn coral, threaded by the teal replay route."
          className="mt-4"
          style={{ display: "block", fontFamily: C.mono }}
        >
          <defs>
            <marker
              id={`${ID}head-teal`}
              markerWidth="6"
              markerHeight="6"
              refX="3"
              refY="2"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 5.5 L3 1 L6 5.5"
                fill="none"
                stroke={C.accent}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
            </marker>
          </defs>

          {/* Grey track. */}
          <line
            x1={RAIL_X0}
            y1={RAIL_Y}
            x2={RAIL_X1}
            y2={RAIL_Y}
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* Registration ticks: the replay frame scale / baseline rhyme. */}
          {Array.from({ length: 13 }, (_, i) => {
            const tx = 44 + (i * (RAIL_X1 - 44)) / 12;
            return (
              <line
                key={`${ID}tick-${i}`}
                x1={tx}
                y1="118"
                x2={tx}
                y2="124"
                stroke={C.inkFaint}
                strokeWidth="1"
                strokeLinecap="round"
                vectorEffect="non-scaling-stroke"
              />
            );
          })}

          {/* Teal thread: the played portion of the replay, from the source ring
              along the track, then up the playhead stem to the handle. */}
          <path
            d={`M${SOURCE_X} ${RAIL_Y} L${PLAYHEAD_X} ${RAIL_Y} L${PLAYHEAD_X} ${
              HANDLE_Y + 5
            }`}
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}head-teal)`}
          />

          {/* Hollow teal source ring: the trace being replayed. */}
          <circle
            cx={SOURCE_X}
            cy={RAIL_Y}
            r="6"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* Resolved hops as frame nodes; cc-surface fill occludes the track. */}
          {HOPS.map((hop) =>
            hop.slow ? null : (
              <circle
                key={hop.label}
                cx={hop.x}
                cy={RAIL_Y}
                r="5"
                fill={C.surface}
                stroke={C.inkFaint}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
            ),
          )}

          {/* The slow billing gRPC hop: coral status node under the playhead. */}
          <circle
            cx={PLAYHEAD_X}
            cy={RAIL_Y}
            r="6"
            fill={C.surface}
            stroke={C.coral}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* Playhead handle: the teal terminal dot parked on the slow hop. */}
          <circle cx={PLAYHEAD_X} cy={HANDLE_Y} r="2.5" fill={C.accent} />

          {/* The trace being replayed. */}
          <text
            x="22"
            y="24"
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.navLabel}
          >
            NITRO TRACE
          </text>
          <text x="22" y="36" fontSize="9" fill={C.ink}>
            4b1c8f2a
          </text>

          {/* The flagged hop and its self-time. */}
          <text
            x={PLAYHEAD_X}
            y="40"
            textAnchor="middle"
            fontSize="9"
            fill={C.coral}
          >
            214ms
          </text>
          <text
            x={PLAYHEAD_X}
            y="92"
            textAnchor="middle"
            fontSize="8"
            fill={C.coral}
          >
            billing gRPC
          </text>
        </svg>

        {/* Single footer numeral: the share of the request the slow hop ate. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            93%
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            of the 231 ms request, in one hop
          </p>
        </div>
      </div>
    </div>
  );
}
