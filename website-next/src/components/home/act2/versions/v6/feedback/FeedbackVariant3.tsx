import type { ReactNode } from "react";

interface FeedbackVariant3Props {
  readonly className?: string;
}

/**
 * Agentic-coding hook, v6 bespoke: an inward hub-and-spoke convergence.
 *
 * Four labeled source nodes (`schema`, `published ops`, `client registry`,
 * `skillz`) feed INWARD along thin violet spokes that brighten toward a central
 * `/graphql/mcp` core. The core is the single grounded surface, so it emits ONE
 * outward teal spoke to a lone `coding agent` node. The violet channel carries
 * the facts your system already proves; the teal channel hands them to the
 * agent. There is no return arc: the agent reads from the surface, it does not
 * negotiate with it.
 *
 * Static React Server Component: no hooks, no client APIs, settled final frame.
 * Dark cc-* palette only, thin 1px strokes, generous negative space. Every svg
 * id is prefixed "v6-feedback-3-".
 */
export function FeedbackVariant3({ className }: FeedbackVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          grounded by your graph
        </p>

        {/* converging radial: four proven sources -> one surface -> one agent */}
        <svg
          viewBox="0 0 336 210"
          width="100%"
          role="img"
          aria-label="Schema, published operations, the client registry, and checked-in skills feeding inward into a /graphql/mcp surface that hands one teal spoke to a coding agent"
          className="mt-4"
          style={{ display: "block", fontFamily: C.mono }}
        >
          <defs>
            {/* Inward violet spokes brighten as they approach the core. */}
            <linearGradient
              id={`${ID}spoke`}
              gradientUnits="userSpaceOnUse"
              x1="110"
              y1="0"
              x2="148"
              y2="0"
            >
              <stop offset="0" stopColor={C.violetDim} stopOpacity="0.4" />
              <stop offset="1" stopColor={C.violet} stopOpacity="0.95" />
            </linearGradient>

            {/* The single outward teal spoke the surface emits. */}
            <linearGradient
              id={`${ID}emit`}
              gradientUnits="userSpaceOnUse"
              x1="210"
              y1="0"
              x2="246"
              y2="0"
            >
              <stop offset="0" stopColor={C.accent} stopOpacity="0.85" />
              <stop offset="1" stopColor={C.accentHover} stopOpacity="1" />
            </linearGradient>

            <radialGradient id={`${ID}core`} cx="38%" cy="28%" r="90%">
              <stop offset="0" stopColor={C.accent} stopOpacity="0.18" />
              <stop offset="65%" stopColor={C.accent} stopOpacity="0" />
            </radialGradient>

            <filter
              id={`${ID}glow`}
              x="-60%"
              y="-60%"
              width="220%"
              height="220%"
            >
              <feGaussianBlur stdDeviation="4.5" />
            </filter>

            <marker
              id={`${ID}vtip`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.6 L5 3 L0 5.4"
                fill="none"
                stroke={C.violet}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>

            <marker
              id={`${ID}ttip`}
              markerWidth="6"
              markerHeight="6"
              refX="5"
              refY="3"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0 0.6 L5 3 L0 5.4"
                fill="none"
                stroke={C.accentHover}
                strokeWidth="1"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>
          </defs>

          {/* four inward violet spokes converging into the core's left face */}
          {SOURCES.map((s) => (
            <path
              key={`spoke-${s.label}`}
              d={`M110 ${s.cy} C 140 ${s.cy} 130 ${s.ey} 148 ${s.ey}`}
              fill="none"
              stroke={`url(#${ID}spoke)`}
              strokeWidth="1.25"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
              markerEnd={`url(#${ID}vtip)`}
            />
          ))}

          {/* the one outward teal spoke from the surface to the agent */}
          <line
            x1="210"
            y1="105"
            x2="244"
            y2="105"
            stroke={`url(#${ID}emit)`}
            strokeWidth="1.5"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            markerEnd={`url(#${ID}ttip)`}
          />
          <circle cx="210" cy="105" r="2.5" fill={C.accent} />

          {/* four labeled source nodes */}
          {SOURCES.map((s) => (
            <g key={`node-${s.label}`}>
              <rect
                x="6"
                y={s.cy - 15}
                width="104"
                height="30"
                rx="9"
                fill={C.surface}
                stroke={C.cardBorder}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <g transform={`translate(13 ${s.cy - 7})`}>{s.icon}</g>
              <text x="34" y={s.cy + 3.2} fontSize="8.3" fill={C.heading}>
                {s.label}
              </text>
              {/* violet port where the spoke leaves the node */}
              <circle cx="110" cy={s.cy} r="2.4" fill={C.violet} />
            </g>
          ))}

          {/* soft teal halo behind the grounded surface */}
          <rect
            x="144"
            y="78"
            width="70"
            height="54"
            rx="14"
            fill="none"
            stroke={C.accent}
            strokeOpacity="0.5"
            strokeWidth="2"
            filter={`url(#${ID}glow)`}
          />

          {/* the central grounded surface: /graphql/mcp */}
          <rect
            x="148"
            y="82"
            width="62"
            height="46"
            rx="12"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
          />
          <rect
            x="148"
            y="82"
            width="62"
            height="46"
            rx="12"
            fill={`url(#${ID}core)`}
          />
          <text
            x="179"
            y="102"
            textAnchor="middle"
            fontSize="8.5"
            fill={C.heading}
          >
            /graphql
          </text>
          <text
            x="179"
            y="114"
            textAnchor="middle"
            fontSize="8.5"
            fill={C.accent}
          >
            /mcp
          </text>

          {/* the single coding agent reading from the surface */}
          <rect
            x="246"
            y="88"
            width="84"
            height="34"
            rx="10"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="262" cy="105" r="2.6" fill={C.accent} />
          <text x="271" y="108.4" fontSize="8.5" fill={C.heading}>
            coding agent
          </text>
        </svg>

        {/* closing stat: proven sources resolve into one grounded surface */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            4 &rarr; 1
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            the agent works from facts your system already proves
          </p>
        </div>
      </div>
    </div>
  );
}

const ID = "v6-feedback-3-";

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, teal, violet. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  accent: "#5eead4",
  accentHover: "#99f6e4",
  violet: "#8b8ff0",
  violetDim: "#7c92c6",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

/** Shared stroke props for the thin violet source glyphs (14x14 local space). */
const GLYPH = {
  fill: "none",
  stroke: C.violet,
  strokeWidth: 1.1,
  strokeLinecap: "round",
  strokeLinejoin: "round",
  vectorEffect: "non-scaling-stroke",
} as const;

/** schema: a type card with field lines. */
const SchemaIcon: ReactNode = (
  <g {...GLYPH}>
    <rect x="3" y="2.5" width="8" height="9" rx="1.5" />
    <line x1="3" y1="5.2" x2="11" y2="5.2" />
    <line x1="5" y1="7.4" x2="9" y2="7.4" />
    <line x1="5" y1="9.2" x2="8" y2="9.2" />
  </g>
);

/** published ops: an operation marked ready to run. */
const PublishedOpsIcon: ReactNode = (
  <g {...GLYPH}>
    <circle cx="7" cy="7" r="5" />
    <path d="M5.7 4.7 L9.6 7 L5.7 9.3 Z" fill={C.violet} stroke="none" />
  </g>
);

/** client registry: a small stack of registered client cards. */
const ClientRegistryIcon: ReactNode = (
  <g {...GLYPH}>
    <rect x="2.5" y="4" width="7.5" height="7.5" rx="1.5" />
    <rect x="4.5" y="2" width="7.5" height="7.5" rx="1.5" fill={C.surface} />
  </g>
);

/** skillz: a checked-in capability spark. */
const SkillzIcon: ReactNode = (
  <path
    d="M7 1.4 L8.2 5.8 L12.6 7 L8.2 8.2 L7 12.6 L5.8 8.2 L1.4 7 L5.8 5.8 Z"
    fill={C.violet}
  />
);

/** The four proven sources, fanning inward into the grounded surface. */
const SOURCES: readonly {
  readonly label: string;
  readonly cy: number;
  readonly ey: number;
  readonly icon: ReactNode;
}[] = [
  { label: "schema", cy: 26, ey: 93, icon: SchemaIcon },
  { label: "published ops", cy: 68, ey: 101, icon: PublishedOpsIcon },
  { label: "client registry", cy: 142, ey: 109, icon: ClientRegistryIcon },
  { label: "skillz", cy: 184, ey: 117, icon: SkillzIcon },
];
