interface BuildIlluProps {
  readonly className?: string;
}

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

/** Section-grade cc-* palette: dark surfaces, cream heading, neutral ink, teal. */
const C = {
  surface: "#0c1322",
  card: "rgba(12, 19, 34, 0.55)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  border: "rgba(245, 241, 234, 0.12)",
  eyebrow: "#62748e",
  accent: "#5eead4",
  csharp: "#512bd4",
} as const;

const ID = "illu-build-";

/** The four artifacts that fan out from the one annotated source class. */
const CHIPS: readonly {
  readonly cat: string;
  readonly name: string;
  readonly cy: number;
}[] = [
  { cat: "SCHEMA", name: "schema.graphql", cy: 71 },
  { cat: "RESOLVERS", name: "resolver pipeline", cy: 109 },
  { cat: "BATCHING", name: "DataLoader", cy: 147 },
  { cat: "CLIENT", name: "typed client", cy: 185 },
];

/**
 * "Lineage fan-out" platform illustration, section grade.
 *
 * One softly-lit `ProductApi.cs` tile on the left carries a restrained
 * `[QueryType]` C# snippet and is the single teal-active node. Four thin teal
 * connector wires fan rightward into a calm column of neutral artifact chips:
 * the schema you serve (`schema.graphql`), the resolvers behind it
 * (`resolver pipeline`), the batching that guards your database (`DataLoader`),
 * and a typed client (`typed client`). Reads "one source, the whole API".
 *
 * Static (no client APIs, settled final frame). Dark cc-* palette only, thin
 * 1px strokes, generous negative space. Every svg id is prefixed "illu-build-".
 */
export function BuildIllu({ className }: BuildIlluProps) {
  return (
    <div
      aria-hidden="true"
      className={["mx-auto w-full select-none", className ?? ""].join(" ")}
    >
      <svg
        viewBox="0 0 448 252"
        width="100%"
        style={{ display: "block", fontFamily: MONO }}
      >
        <defs>
          <linearGradient
            id={`${ID}wire`}
            gradientUnits="userSpaceOnUse"
            x1="204"
            y1="0"
            x2="272"
            y2="0"
          >
            <stop offset="0" stopColor={C.accent} stopOpacity="0.9" />
            <stop offset="1" stopColor={C.accent} stopOpacity="0.5" />
          </linearGradient>

          <radialGradient id={`${ID}lit`} cx="30%" cy="22%" r="92%">
            <stop offset="0" stopColor={C.accent} stopOpacity="0.16" />
            <stop offset="62%" stopColor={C.accent} stopOpacity="0" />
          </radialGradient>

          <filter id={`${ID}glow`} x="-40%" y="-40%" width="180%" height="180%">
            <feGaussianBlur stdDeviation="5" />
          </filter>

          <marker
            id={`${ID}arrow`}
            markerWidth="7"
            markerHeight="7"
            refX="5"
            refY="3"
            orient="auto"
            markerUnits="userSpaceOnUse"
          >
            <path
              d="M0 0.6 L5 3 L0 5.4"
              fill="none"
              stroke={C.accent}
              strokeWidth="1.1"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </marker>
        </defs>

        {/* card panel */}
        <rect
          x="0.5"
          y="0.5"
          width="447"
          height="251"
          rx="18"
          fill={C.card}
          stroke={C.border}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />

        {/* eyebrow */}
        <text x="28" y="34" fontSize="9.5" letterSpacing="1.8" fill={C.eyebrow}>
          ONE SOURCE, THE WHOLE API
        </text>

        {/* four teal connector wires fanning from the source node */}
        {CHIPS.map((chip) => (
          <path
            key={`wire-${chip.name}`}
            d={`M204 130 C 240 130 240 ${chip.cy} 272 ${chip.cy}`}
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
          x="28"
          y="80"
          width="176"
          height="100"
          rx="14"
          fill="none"
          stroke={C.accent}
          strokeOpacity="0.45"
          strokeWidth="2"
          filter={`url(#${ID}glow)`}
        />

        {/* the one annotated source class, the single teal-active node */}
        <rect
          x="28"
          y="80"
          width="176"
          height="100"
          rx="14"
          fill={C.surface}
          stroke={C.accent}
          strokeWidth="1.25"
          vectorEffect="non-scaling-stroke"
        />
        <rect
          x="28"
          y="80"
          width="176"
          height="100"
          rx="14"
          fill={`url(#${ID}lit)`}
        />

        {/* tile header: C# glyph + file name */}
        <rect x="44" y="94" width="16" height="16" rx="4" fill={C.csharp} />
        <text
          x="52"
          y="105.5"
          textAnchor="middle"
          fontSize="8.5"
          fontWeight="700"
          fill="#ffffff"
        >
          C#
        </text>
        <text x="68" y="106" fontSize="10" fill={C.heading}>
          ProductApi.cs
        </text>

        <line
          x1="44"
          y1="120"
          x2="188"
          y2="120"
          stroke={C.border}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />

        {/* restrained C# snippet */}
        <text x="44" y="140" fontSize="10" fill={C.accent}>
          [QueryType]
        </text>
        <text x="44" y="157" fontSize="9" fill={C.ink}>
          public Product
        </text>
        <text x="44" y="173" fontSize="9" fill={C.inkDim}>
          GetProduct(int id)
        </text>

        {/* source emanation node */}
        <circle
          cx="204"
          cy="130"
          r="7"
          fill={C.accent}
          opacity="0.16"
          filter={`url(#${ID}glow)`}
        />
        <circle cx="204" cy="130" r="3.5" fill={C.accent} />

        {/* four neutral artifact chips */}
        {CHIPS.map((chip) => (
          <g key={chip.name}>
            <rect
              x="278"
              y={chip.cy - 16}
              width="146"
              height="32"
              rx="9"
              fill={C.surface}
              stroke={C.border}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx="278" cy={chip.cy} r="2" fill={C.accent} opacity="0.7" />
            <text
              x="292"
              y={chip.cy - 3}
              fontSize="7"
              letterSpacing="1.3"
              fill={C.eyebrow}
            >
              {chip.cat}
            </text>
            <text x="292" y={chip.cy + 10} fontSize="10" fill={C.heading}>
              {chip.name}
            </text>
          </g>
        ))}

        {/* footer */}
        <line
          x1="28"
          y1="216"
          x2="420"
          y2="216"
          stroke={C.border}
          strokeWidth="1"
          vectorEffect="non-scaling-stroke"
        />
        <text
          x="224"
          y="234"
          textAnchor="middle"
          fontSize="8"
          letterSpacing="0.4"
          fill={C.inkDim}
        >
          one annotated class stays in step with every artifact
        </text>
      </svg>
    </div>
  );
}
