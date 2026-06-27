interface BuildVariant3Props {
  readonly className?: string;
}

/**
 * "Build loop" scene, v5 "Schematic Lines", variant 3: "Source-gen build pass".
 *
 * A reductive monoline schematic of the source-generation pass that runs during
 * `dotnet build`. The grey skeleton is a LINEAR pass-pipeline chain of the
 * ordered build stages (restore -> source-gen -> compile) drawn as open 1px
 * circles on a 1px wire. The single teal thread is the one route the headline
 * names: it begins at the hollow teal source-gen ring (the active pass) and
 * drops straight down to the emitted `schema.graphql` terminal, a teal focal
 * ring with a solid teal dot. A faint strip of registration ticks acts as the
 * build's baseline/scale. The whole thing floats on the shared sibling card with
 * no inner panel.
 *
 * cc-* palette only; everything is 1px non-scaling stroke. Exactly one teal
 * accent (the thread) and no status hue. RSC, settled final frame, no motion,
 * no hooks. Every svg id is prefixed "v5-build-3-".
 */

const C = {
  surface: "#0c1322",
  ink: "#a1a3af",
  navLabel: "#62748e",
  inkFaint: "rgba(245,241,234,0.16)",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-build-3-";

// Ordered build stages, left to right. source-gen is the active pass and carries
// the teal source ring; restore and compile stay grey open nodes.
const STAGES: readonly {
  readonly label: string;
  readonly cx: number;
  readonly r: number;
  readonly accent: boolean;
}[] = [
  { label: "RESTORE", cx: 46, r: 8, accent: false },
  { label: "SOURCE-GEN", cx: 140, r: 11, accent: true },
  { label: "COMPILE", cx: 234, r: 8, accent: false },
];

export function BuildVariant3({ className }: BuildVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          source-gen build pass
        </p>

        <div className="mt-4">
          <svg
            viewBox="0 0 280 150"
            width="100%"
            style={{ display: "block", overflow: "visible" }}
          >
            <defs>
              {/* Teal open chevron for the single thread terminus. */}
              <marker
                id={`${ID}teal`}
                markerWidth="6"
                markerHeight="6"
                refX="4.6"
                refY="3"
                orient="auto"
                markerUnits="userSpaceOnUse"
              >
                <path
                  d="M0 0.5 L5 3 L0 5.5"
                  fill="none"
                  stroke={C.accent}
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </marker>
            </defs>

            {/* ---- grey skeleton: linear pass-pipeline chain ---- */}
            <line
              x1="54"
              y1="40"
              x2="129"
              y2="40"
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
            />
            <line
              x1="151"
              y1="40"
              x2="226"
              y2="40"
              stroke={C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
            />

            {STAGES.map((stage) => (
              <g key={`${ID}stage-${stage.label}`}>
                <circle
                  cx={stage.cx}
                  cy={40}
                  r={stage.r}
                  fill="none"
                  stroke={stage.accent ? C.accent : C.inkFaint}
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                />
                <text
                  x={stage.cx}
                  y={60}
                  textAnchor="middle"
                  fontFamily={C.mono}
                  fontSize="7"
                  letterSpacing="0.08em"
                  fill={C.navLabel}
                >
                  {stage.label}
                </text>
              </g>
            ))}

            {/* ---- teal thread: source-gen pass emits the schema ---- */}
            <line
              x1="140"
              y1="51"
              x2="140"
              y2="86"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
              strokeLinecap="round"
              markerEnd={`url(#${ID}teal)`}
            />
            <circle
              cx={140}
              cy={96}
              r={8}
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
            <circle cx={140} cy={96} r={2.5} fill={C.accent} />
            <text
              x={140}
              y={114}
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize="8"
              fill={C.ink}
            >
              schema.graphql
            </text>

            {/* registration ticks: the build's baseline / scale */}
            {Array.from({ length: 13 }, (_, i) => {
              const x = 44 + i * 16;
              return (
                <line
                  key={`${ID}tick-${i}`}
                  x1={x}
                  y1={126}
                  x2={x}
                  y2={131}
                  stroke={C.inkFaint}
                  strokeWidth="1"
                  vectorEffect="non-scaling-stroke"
                  strokeLinecap="round"
                />
              );
            })}
          </svg>
        </div>

        {/* Single footer: the load-bearing emit cost (kept off the build total). */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            0.4s
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">schema emitted</p>
        </div>
      </div>
    </div>
  );
}
