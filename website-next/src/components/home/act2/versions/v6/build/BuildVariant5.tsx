/**
 * v6 "Build loop" hook illustration (variant 5): one source of truth, not four.
 *
 * A bespoke before/after collapse. On the left, four loosely-pinned DASHED file
 * tiles (schema.graphql, Resolvers.cs, client.schema, mappings) are tangled by
 * fraying grey sync arrows, each drifting at its own slight angle. A teal
 * collapse arrow crosses a faint divider into ONE solid teal-edged ProductApi.cs
 * tile on the right, where the four concerns now live as annotations on a single
 * class, all in step. Many dashed in, one solid out.
 *
 * Static React Server Component: no hooks, no motion. Pure inline SVG using the
 * site's cc-* palette; every id is prefixed `v6-build-5-`.
 */

interface BuildVariant5Props {
  readonly className?: string;
}

const C = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  dim: "rgba(245,241,234,0.62)",
  eyebrow: "#62748e",
  border: "rgba(245,241,234,0.12)",
  fray: "rgba(245,241,234,0.30)",
  accent: "#5eead4",
  healthy: "#34d399",
} as const;

const MONO =
  "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace";

const FILES: readonly {
  readonly x: number;
  readonly y: number;
  readonly rot: number;
  readonly label: string;
}[] = [
  { x: 16, y: 18, rot: -4, label: "schema.graphql" },
  { x: 40, y: 49, rot: 3, label: "Resolvers.cs" },
  { x: 12, y: 84, rot: -2.5, label: "client.schema" },
  { x: 44, y: 116, rot: 3.5, label: "mappings" },
];

const CONCERNS: readonly {
  readonly x: number;
  readonly y: number;
  readonly label: string;
}[] = [
  { x: 212, y: 116, label: "schema" },
  { x: 272, y: 116, label: "resolvers" },
  { x: 212, y: 131, label: "client" },
  { x: 272, y: 131, label: "mappings" },
];

export function BuildVariant5({ className }: BuildVariant5Props) {
  return (
    <div
      className={["mx-auto flex w-full max-w-[340px] justify-center", className ?? ""].join(
        " ",
      )}
    >
      <svg
        viewBox="0 0 340 156"
        role="img"
        aria-label="Four drifting source files collapse into one annotated ProductApi.cs class"
        style={{
          width: "100%",
          height: "auto",
          fontFamily: MONO,
          display: "block",
        }}
      >
        {/* eyebrows */}
        <text
          x={14}
          y={11}
          fontSize={7}
          letterSpacing="0.14em"
          fill={C.eyebrow}
        >
          OUT OF STEP
        </text>
        <text
          x={200}
          y={29}
          fontSize={7}
          letterSpacing="0.14em"
          fill={C.eyebrow}
        >
          SOURCE OF TRUTH
        </text>

        {/* fraying grey sync arrows between the loosely-pinned tiles */}
        <g
          stroke={C.fray}
          strokeWidth={1}
          strokeDasharray="3 3"
          fill="none"
          strokeLinecap="round"
        >
          <path d="M74 42 L80 51" />
          <path d="M68 71 L60 85" />
          <path d="M70 108 L80 119" />
          <path d="M48 41 Q38 64 44 84" opacity={0.55} strokeDasharray="2 4" />
        </g>
        <g fill={C.fray}>
          <polygon points="76,47 81,52 78,46" />
          <polygon points="62,80 59,86 64,84" />
          <polygon points="76,114 81,120 78,113" />
        </g>

        {/* four dashed, loosely-pinned file tiles */}
        {FILES.map((f) => {
          const cx = f.x + 46;
          const cy = f.y + 12;
          const gx = f.x + 9;
          const gy = f.y + 6;
          return (
            <g key={f.label} transform={`rotate(${f.rot} ${cx} ${cy})`}>
              <rect
                x={f.x}
                y={f.y}
                width={92}
                height={24}
                rx={4}
                fill={C.surface}
                stroke={C.border}
                strokeWidth={1}
                strokeDasharray="4 3"
              />
              <circle cx={f.x + 4} cy={f.y + 4} r={1.6} fill={C.eyebrow} />
              <path
                d={`M${gx} ${gy} h5 l3 3 v8 h-8 z`}
                fill="none"
                stroke={C.dim}
                strokeWidth={0.9}
                strokeLinejoin="round"
              />
              <path
                d={`M${gx + 5} ${gy} v3 h3`}
                fill="none"
                stroke={C.dim}
                strokeWidth={0.9}
                strokeLinejoin="round"
              />
              <text x={f.x + 24} y={f.y + 16} fontSize={7.5} fill={C.ink}>
                {f.label}
              </text>
            </g>
          );
        })}

        {/* faint divider + bold teal collapse arrow */}
        <line
          x1={150}
          y1={20}
          x2={150}
          y2={140}
          stroke={C.border}
          strokeWidth={1}
          strokeDasharray="2 6"
          opacity={0.6}
        />
        <text
          x={172}
          y={80}
          fontSize={7}
          letterSpacing="0.12em"
          textAnchor="middle"
          fill={C.eyebrow}
        >
          COLLAPSE
        </text>
        <line x1={151} y1={88} x2={191} y2={88} stroke={C.accent} strokeWidth={1.6} />
        <polygon points="190,84 197,88 190,92" fill={C.accent} />

        {/* one solid teal-edged class tile */}
        <rect
          x={199}
          y={37}
          width={132}
          height={102}
          rx={9}
          fill="none"
          stroke={C.accent}
          strokeWidth={3}
          opacity={0.14}
        />
        <rect
          x={201}
          y={39}
          width={128}
          height={98}
          rx={7}
          fill={C.surface}
          stroke={C.accent}
          strokeWidth={1.3}
        />

        {/* header: file glyph + name + in-step check */}
        <path
          d="M213 48 h6 l3 3 v9 h-9 z"
          fill="none"
          stroke={C.accent}
          strokeWidth={1}
          strokeLinejoin="round"
        />
        <path
          d="M219 48 v3 h3"
          fill="none"
          stroke={C.accent}
          strokeWidth={1}
          strokeLinejoin="round"
        />
        <text x={228} y={57} fontSize={9.5} fill={C.heading}>
          ProductApi.cs
        </text>
        <polyline
          points="312,51 315,54 320,47"
          fill="none"
          stroke={C.healthy}
          strokeWidth={1.4}
          strokeLinecap="round"
          strokeLinejoin="round"
        />

        <line x1={209} y1={65} x2={321} y2={65} stroke={C.border} strokeWidth={1} />

        {/* annotated class */}
        <text x={213} y={83} fontSize={8} fill={C.accent}>
          [QueryType]
        </text>
        <text x={213} y={98} fontSize={8.5}>
          <tspan fill={C.dim}>class </tspan>
          <tspan fill={C.heading}>ProductApi</tspan>
        </text>

        {/* the four concerns, now unified and in step */}
        {CONCERNS.map((c) => (
          <g key={c.label}>
            <polyline
              points={`${c.x},${c.y - 3} ${c.x + 2.5},${c.y - 0.5} ${c.x + 6},${c.y - 5.5}`}
              fill="none"
              stroke={C.accent}
              strokeWidth={1.1}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <text x={c.x + 10} y={c.y} fontSize={7} fill={C.dim}>
              {c.label}
            </text>
          </g>
        ))}
      </svg>
    </div>
  );
}
