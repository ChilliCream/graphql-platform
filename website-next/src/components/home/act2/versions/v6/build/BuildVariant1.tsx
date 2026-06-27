interface BuildVariant1Props {
  readonly className?: string;
}

/**
 * "Build loop" hook illustration, v6 bespoke: radial lineage fan-out.
 *
 * One softly-lit `[QueryType] ProductApi.cs` tile on the left is the single
 * source and the only teal-active node. Four clean teal connector wires fan
 * rightward into four neutral artifact chips: the schema you serve
 * (`schema.graphql`), the resolvers behind it (`resolver pipeline`), the
 * batching that guards your database (`DataLoader`), and a typed .NET client
 * (`typed client`). One annotated C# class, four shipped artifacts.
 *
 * Static React Server Component: no hooks, no client APIs, settled final frame.
 * Dark cc-* palette only; every svg id is prefixed "v6-build-1-".
 */
export function BuildVariant1({ className }: BuildVariant1Props) {
  const ID = "v6-build-1-";

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          one class, four artifacts
        </p>

        {/* radial fan-out: one softly-lit source tile emits four artifact chips */}
        <svg
          viewBox="0 0 332 198"
          width="100%"
          role="img"
          aria-label="One annotated C# class fanning out into a schema, a resolver pipeline, a DataLoader, and a typed client"
          className="mt-4"
          style={{ display: "block", fontFamily: MONO }}
        >
          <defs>
            <linearGradient
              id={`${ID}wire`}
              gradientUnits="userSpaceOnUse"
              x1="118"
              y1="0"
              x2="189"
              y2="0"
            >
              <stop offset="0" stopColor={C.accent} stopOpacity="0.95" />
              <stop offset="1" stopColor={C.accent} stopOpacity="0.5" />
            </linearGradient>

            <radialGradient id={`${ID}lit`} cx="32%" cy="24%" r="92%">
              <stop offset="0" stopColor={C.accent} stopOpacity="0.16" />
              <stop offset="60%" stopColor={C.accent} stopOpacity="0" />
            </radialGradient>

            <filter
              id={`${ID}glow`}
              x="-40%"
              y="-40%"
              width="180%"
              height="180%"
            >
              <feGaussianBlur stdDeviation="5" />
            </filter>

            <marker
              id={`${ID}arrow`}
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
              />
            </marker>
          </defs>

          {/* four teal connector wires fanning from the source node */}
          {CHIPS.map((chip) => (
            <path
              key={`wire-${chip.name}`}
              d={`M118 99 C 156 99 156 ${chip.cy} 189 ${chip.cy}`}
              fill="none"
              stroke={`url(#${ID}wire)`}
              strokeWidth="1.25"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
              markerEnd={`url(#${ID}arrow)`}
            />
          ))}

          {/* soft teal halo behind the active source tile */}
          <rect
            x="10"
            y="58"
            width="108"
            height="82"
            rx="10"
            fill="none"
            stroke={C.accent}
            strokeOpacity="0.5"
            strokeWidth="2"
            filter={`url(#${ID}glow)`}
          />

          {/* the one annotated source class, the only teal-active node */}
          <rect
            x="10"
            y="58"
            width="108"
            height="82"
            rx="10"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1.25"
            vectorEffect="non-scaling-stroke"
          />
          <rect
            x="10"
            y="58"
            width="108"
            height="82"
            rx="10"
            fill={`url(#${ID}lit)`}
          />

          {/* tile header: C# glyph + file name */}
          <rect x="20" y="68" width="15" height="15" rx="3" fill="#512bd4" />
          <text
            x="27.5"
            y="78.6"
            textAnchor="middle"
            fontSize="8.5"
            fontWeight="700"
            fill="#ffffff"
          >
            C#
          </text>
          <text x="41" y="79" fontSize="8.5" fill={C.heading}>
            ProductApi.cs
          </text>

          <line
            x1="20"
            y1="90"
            x2="108"
            y2="90"
            stroke={C.cardBorder}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          <text x="20" y="107" fontSize="9" fill={C.accent}>
            [QueryType]
          </text>
          <text x="20" y="123" fontSize="8.5" fill={C.inkDim}>
            class ProductApi
          </text>

          {/* source emanation node */}
          <circle cx="118" cy="99" r="3" fill={C.accent} />

          {/* four neutral artifact chips */}
          {CHIPS.map((chip) => (
            <g key={chip.name}>
              <rect
                x="190"
                y={chip.cy - 18}
                width="132"
                height="36"
                rx="9"
                fill={C.surface}
                stroke={C.cardBorder}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
              <text
                x="204"
                y={chip.cy - 3}
                fontSize="7"
                letterSpacing="1.4"
                fill={C.navLabel}
              >
                {chip.cat}
              </text>
              <text x="204" y={chip.cy + 10} fontSize="9.5" fill={C.heading}>
                {chip.name}
              </text>
            </g>
          ))}
        </svg>

        {/* closing stat: one source, four shipped artifacts */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            1 &rarr; 4
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            one annotated class, four shipped artifacts
          </p>
        </div>
      </div>
    </div>
  );
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** The four artifacts that fan out from the one annotated source class. */
const CHIPS: readonly {
  readonly cat: string;
  readonly name: string;
  readonly cy: number;
}[] = [
  { cat: "SCHEMA", name: "schema.graphql", cy: 30 },
  { cat: "RESOLVERS", name: "resolver pipeline", cy: 76 },
  { cat: "BATCHING", name: "DataLoader", cy: 122 },
  { cat: "CLIENT", name: "typed client", cy: 168 },
];

/** Locked v6 cc-* palette for this cell: dark surfaces, neutral ink, teal. */
const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  heading: "#f5f0ea",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;
