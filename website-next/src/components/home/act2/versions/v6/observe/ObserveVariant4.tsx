interface ObserveVariant4Props {
  readonly className?: string;
}

/**
 * "Production view" hook illustration, v6 bespoke: a downward-fanning service
 * topology.
 *
 * A `GraphQL` gateway node at the top branches down into four calm teal-outlined
 * service boxes: a `REST` users service, a `gRPC` billing service, a background
 * `job` worker, and a `DB`. Hairline grey edges wire the fan together. The one
 * node-and-edge that is degrading, `GraphQL -> gRPC` billing, is lit amber with a
 * soft glow so the eye lands on the cause. Amber here reads as degrading; coral
 * stays reserved for the confirmed-slow tile, so the two graph-like tiles stay
 * distinct.
 *
 * Static React Server Component: no hooks, no client APIs, settled final frame.
 * Dark cc-* palette only; teal outlines healthy hops, amber marks the degrading
 * hop. Every svg id is prefixed "v6-observe-4-".
 */
export function ObserveVariant4({ className }: ObserveVariant4Props) {
  const ID = "v6-observe-4-";
  const degrading = SERVICES.find((s) => s.tone === "degrading") ?? SERVICES[1];

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* header: map title on the left, the one-degrading cue on the right */}
        <div className="flex items-center justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            service map
          </p>
          <span
            className="inline-flex items-center gap-1.5 font-mono text-[0.6rem] whitespace-nowrap"
            style={{
              borderRadius: 999,
              border: `1px solid ${C.amber}66`,
              background: `${C.amber}1a`,
              color: C.amber,
              padding: "3px 9px",
              letterSpacing: "0.04em",
            }}
          >
            <span
              aria-hidden="true"
              style={{
                display: "inline-block",
                width: 6,
                height: 6,
                borderRadius: 999,
                background: C.amber,
              }}
            />
            1 degrading
          </span>
        </div>

        {/* topology canvas on a faint dot grid */}
        <div
          className="relative mt-4 overflow-hidden rounded-xl border"
          style={{ borderColor: C.cardBorder, background: C.canvas }}
        >
          <div
            aria-hidden="true"
            className="absolute inset-0"
            style={{
              backgroundImage: `radial-gradient(${C.dot} 1px, transparent 1px)`,
              backgroundSize: "16px 16px",
              opacity: 0.55,
            }}
          />

          <svg
            viewBox="0 0 320 200"
            width="100%"
            role="img"
            aria-label="GraphQL gateway fanning down to a REST users service, a gRPC billing service, a job worker, and a database. The GraphQL to gRPC billing hop is degrading and lit amber."
            style={{ position: "relative", display: "block", fontFamily: MONO }}
          >
            <defs>
              <filter
                id={`${ID}glow`}
                x="-60%"
                y="-60%"
                width="220%"
                height="220%"
              >
                <feGaussianBlur stdDeviation="4" />
              </filter>

              <marker
                id={`${ID}arrow`}
                markerWidth="6"
                markerHeight="6"
                refX="4.5"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0 0.5 L5 3 L0 5.5"
                  fill="none"
                  stroke="rgba(245, 241, 234, 0.4)"
                  strokeWidth="1"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </marker>

              <marker
                id={`${ID}arrow-amber`}
                markerWidth="7"
                markerHeight="7"
                refX="4.5"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0 0.5 L5 3 L0 5.5"
                  fill="none"
                  stroke={C.amber}
                  strokeWidth="1.2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </marker>
            </defs>

            {/* hairline grey edges for the healthy hops */}
            {SERVICES.filter((s) => s.tone !== "degrading").map((s) => (
              <path
                key={`edge-${s.kind}`}
                d={edgePath(s.cx)}
                fill="none"
                stroke={C.edge}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
                markerEnd={`url(#${ID}arrow)`}
              />
            ))}

            {/* the degrading hop: soft amber glow under a crisp amber edge */}
            <path
              d={edgePath(degrading.cx)}
              fill="none"
              stroke={C.amber}
              strokeWidth="4"
              strokeOpacity="0.45"
              strokeLinecap="round"
              filter={`url(#${ID}glow)`}
            />
            <path
              d={edgePath(degrading.cx)}
              fill="none"
              stroke={C.amber}
              strokeWidth="1.6"
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
              markerEnd={`url(#${ID}arrow-amber)`}
            />

            {/* amber halo behind the degrading node */}
            <rect
              x={degrading.cx - 34}
              y={NODE_Y}
              width={NODE_W}
              height={NODE_H}
              rx="8"
              fill="none"
              stroke={C.amber}
              strokeWidth="3"
              strokeOpacity="0.5"
              filter={`url(#${ID}glow)`}
            />

            {/* the four service nodes */}
            {SERVICES.map((s) => {
              const deg = s.tone === "degrading";
              const x = s.cx - 34;
              return (
                <g key={s.kind}>
                  <rect
                    x={x}
                    y={NODE_Y}
                    width={NODE_W}
                    height={NODE_H}
                    rx="8"
                    fill={C.surface}
                    stroke={deg ? C.amber : C.accent}
                    strokeOpacity={deg ? 1 : 0.4}
                    strokeWidth="1.25"
                    vectorEffect="non-scaling-stroke"
                  />
                  {deg && (
                    <rect
                      x={x}
                      y={NODE_Y}
                      width={NODE_W}
                      height={NODE_H}
                      rx="8"
                      fill={C.amber}
                      fillOpacity="0.07"
                    />
                  )}
                  <circle
                    cx={x + 12}
                    cy={NODE_Y + 23}
                    r="3.5"
                    fill={deg ? C.amber : C.accent}
                  />
                  <text
                    x={x + 22}
                    y={NODE_Y + 19}
                    fontSize="10"
                    fontWeight="600"
                    fill={deg ? C.amber : C.heading}
                  >
                    {s.kind}
                  </text>
                  <text x={x + 22} y={NODE_Y + 33} fontSize="8" fill={C.inkDim}>
                    {s.name}
                  </text>
                </g>
              );
            })}

            {/* the GraphQL gateway, root of the fan */}
            <rect
              x="112"
              y="12"
              width="96"
              height="38"
              rx="9"
              fill={C.surface}
              stroke={C.accent}
              strokeOpacity="0.5"
              strokeWidth="1.25"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx="124" cy="31" r="3.5" fill={C.accent} />
            <text
              x="135"
              y="28"
              fontSize="11"
              fontWeight="600"
              fill={C.heading}
            >
              GraphQL
            </text>
            <text x="135" y="41" fontSize="8" fill={C.inkDim}>
              gateway
            </text>
          </svg>
        </div>

        {/* footer: name the degrading hop and its latency */}
        <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-4">
          <span className="font-mono text-[0.7rem]" style={{ color: C.inkDim }}>
            <span style={{ color: C.heading }}>GraphQL</span>
            <span style={{ color: C.amber }}> &rarr; </span>
            <span style={{ color: C.heading }}>billing</span>
          </span>
          <span
            className="font-mono text-[0.6rem] whitespace-nowrap"
            style={{
              borderRadius: 999,
              border: `1px solid ${C.amber}66`,
              background: `${C.amber}1a`,
              color: C.amber,
              padding: "2px 8px",
            }}
          >
            p99 +210ms
          </span>
        </div>
      </div>
    </div>
  );
}

/** Bottom-center of the GraphQL root fanning down to a child node's top-center. */
function edgePath(cx: number): string {
  return `M160 50 C 160 102 ${cx} 98 ${cx} 150`;
}

const NODE_W = 68;
const NODE_H = 42;
const NODE_Y = 150;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

type ServiceTone = "healthy" | "degrading";

/** The four downstream hops the GraphQL gateway fans out to. */
const SERVICES: readonly {
  readonly kind: string;
  readonly name: string;
  readonly cx: number;
  readonly tone: ServiceTone;
}[] = [
  { kind: "REST", name: "users-svc", cx: 43, tone: "healthy" },
  { kind: "gRPC", name: "billing", cx: 121, tone: "degrading" },
  { kind: "job", name: "worker", cx: 199, tone: "healthy" },
  { kind: "DB", name: "orders-db", cx: 277, tone: "healthy" },
];

/** Locked v6 cc-* palette for this cell: dark surfaces, teal, degrading amber. */
const C = {
  surface: "#0c1322",
  canvas: "rgba(11, 15, 26, 0.55)",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  amber: "#fbbf24",
  edge: "rgba(245, 241, 234, 0.18)",
  dot: "rgba(98, 116, 142, 0.4)",
} as const;
