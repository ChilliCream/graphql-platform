interface BuildVariant2Props {
  readonly className?: string;
}

/**
 * "Build loop" scene, v4 "Generated Artifacts", variant 2: DataLoader request
 * collapsing.
 *
 * Topology D (convergence fan) drawn inside one generated-artifact tile, the
 * emitted `ProductDataLoader.g.cs`. Six inbound `LoadAsync(id)` key-requests that
 * arrive within a single tick are shown as a left column of key chips (7, 7, 12,
 * 7, 12, 4); the three duplicate keys are dimmed to cc-ink-faint to read as
 * deduped away. Six 1px grey fan leaders converge to one merge dot, and a single
 * grey route exits into the batched call `Fetch([7, 12, 4])`. The only teal in
 * the figure is the deduped key array (the one load-bearing token) plus its
 * signature callout: a 2.5px dot, a 1px leader dropping into the lower-right
 * negative space, and a "BATCHED" micro-label from the fixed verb set. Remove the
 * teal cluster and the figure is neutral grey. A Stat duo footer reports the
 * 6-keys / 1-fetch collapse; the cream numerals live only there.
 *
 * Borrowed literals: `ProductDataLoader` and `LoadAsync(...)` from the v1 / v2
 * build siblings. React Server Component, no hooks, settled final frame only.
 * Every svg id is prefixed "v4-build-2-".
 */

const CC = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  inkFaint: "rgba(245,241,234,0.16)",
  cardBorder: "rgba(245,241,234,0.12)",
  navLabel: "#62748e",
  accent: "#5eead4",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-build-2-";

// Six inbound key-requests in one tick. The first occurrence of each key is
// kept; later duplicates are dimmed (deduped away). Unique set: 7, 12, 4.
const KEYS: readonly { readonly id: string; readonly dup: boolean }[] = [
  { id: "7", dup: false },
  { id: "7", dup: true },
  { id: "12", dup: false },
  { id: "7", dup: true },
  { id: "12", dup: true },
  { id: "4", dup: false },
];

export function BuildVariant2({ className }: BuildVariant2Props) {
  const rowY = (i: number) => 60 + i * 19;
  const mergeX = 150;
  const mergeY = 107.5;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          request collapsing
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 188"
            width="100%"
            role="img"
            aria-label="Six LoadAsync key-requests in one tick collapsing into a single batched, deduplicated fetch inside the generated ProductDataLoader."
            style={{ display: "block" }}
          >
            <defs>
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
                  stroke={CC.ink}
                  strokeWidth={1}
                  strokeOpacity={0.75}
                />
              </marker>
            </defs>

            {/* the generated DataLoader artifact tile */}
            <rect
              x={4}
              y={2}
              width={312}
              height={184}
              rx={8}
              fill={CC.surface}
              stroke={CC.cardBorder}
              strokeWidth={1}
            />

            {/* header band: filename left (inkDim), extension right (navLabel) */}
            <text
              x={16}
              y={18}
              fill={CC.inkDim}
              fontFamily={MONO}
              fontSize={8.5}
            >
              ProductDataLoader
            </text>
            <text
              x={304}
              y={18}
              textAnchor="end"
              fill={CC.navLabel}
              fontFamily={MONO}
              fontSize={7}
              letterSpacing="0.05em"
            >
              .g.cs
            </text>
            <line
              x1={4}
              y1={26}
              x2={316}
              y2={26}
              stroke={CC.cardBorder}
              strokeWidth={1}
            />

            {/* descriptor for the inbound calls gathered this tick */}
            <text
              x={16}
              y={44}
              fill={CC.navLabel}
              fontFamily={MONO}
              fontSize={7.5}
            >
              6x LoadAsync(id)
            </text>

            {/* convergence fan: six leaders into one merge dot (duplicates dim) */}
            {KEYS.map((k, i) => (
              <line
                key={`${ID}fan-${i}`}
                x1={54}
                y1={rowY(i)}
                x2={mergeX}
                y2={mergeY}
                stroke={k.dup ? CC.inkFaint : CC.ink}
                strokeOpacity={k.dup ? 1 : 0.85}
                strokeWidth={1}
              />
            ))}
            <circle
              cx={mergeX}
              cy={mergeY}
              r={2}
              fill={CC.ink}
              fillOpacity={0.7}
            />

            {/* the single batched route out of the merge into the fetch call */}
            <line
              x1={mergeX}
              y1={mergeY}
              x2={174}
              y2={mergeY}
              stroke={CC.ink}
              strokeOpacity={0.75}
              strokeWidth={1}
              markerEnd={`url(#${ID}arrow)`}
            />

            {/* six inbound key chips (duplicate keys dimmed to read as deduped) */}
            {KEYS.map((k, i) => (
              <g key={`${ID}key-${i}`}>
                <rect
                  x={14}
                  y={rowY(i) - 6}
                  width={40}
                  height={12}
                  rx={3}
                  fill={CC.surface}
                  stroke={k.dup ? CC.inkFaint : CC.cardBorder}
                  strokeWidth={1}
                />
                <text
                  x={34}
                  y={rowY(i) + 3}
                  textAnchor="middle"
                  fill={k.dup ? CC.inkFaint : CC.ink}
                  fontFamily={MONO}
                  fontSize={8.5}
                >
                  {k.id}
                </text>
              </g>
            ))}

            {/* batched fetch: one call, the deduped key array highlighted teal */}
            <text
              x={178}
              y={mergeY + 3.5}
              fill={CC.ink}
              fontFamily={MONO}
              fontSize={9.5}
            >
              Fetch(
              <tspan fill={CC.accent}>[7, 12, 4]</tspan>)
            </text>

            {/* signature callout: dot on the deduped token, leader dropping into
                the lower-right gap, "BATCHED" micro-label from the verb set */}
            <circle cx={224} cy={117} r={2.5} fill={CC.accent} />
            <path
              d="M224 117 V150"
              fill="none"
              stroke={CC.accent}
              strokeWidth={1}
            />
            <text
              x={224}
              y={162}
              textAnchor="middle"
              fill={CC.accent}
              fontFamily={MONO}
              fontSize={7}
              letterSpacing="0.08em"
            >
              BATCHED
            </text>
          </svg>
        </div>

        {/* Stat duo footer: the collapse, six keys to one batched fetch */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              6
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">keys this tick</p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">batched fetch</p>
          </div>
        </div>
      </div>
    </div>
  );
}
