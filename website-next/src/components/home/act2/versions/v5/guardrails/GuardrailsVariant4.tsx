interface GuardrailsVariant4Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, v5 "Schematic Lines", concept #4: generated client
 * build drift.
 *
 * Two parallel rails that diverge with a measured drift gap. On the left a teal
 * source ring starts the regenerated Strawberry Shake client, and its teal thread
 * runs flat, coincident with the grey `int` baseline the hand-written consumer
 * code expects (the two rails are in sync, registration ticks mark the locked
 * contract). At the dashed schema-change boundary the server retypes
 * `Product.rating` from `Int!` to `Float`, so the generated property becomes
 * `double?` and the rail drifts away in coral (the one real status: breaking).
 * The drift lands on a coral focal node, the catch, and a coral dimension bracket
 * measures the gap to the expected `int` slot still sitting on the grey baseline.
 * Strip the teal and coral and the schematic reads as two quiet grey rails; the
 * eye lands on the one type that no longer fits.
 *
 * Borrowed content (exact, from the v2 / Nitro siblings): `dotnet build` for the
 * EShops storefront whose Strawberry Shake client was regenerated against a schema
 * where `Product.rating` went `Int! -> Float`, so the generated `double?` no longer
 * fits the `int` it is assigned to, surfacing as a `CS0266` compile error.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only.
 * Exactly one teal accent (the in-sync client thread + source ring) and one status
 * hue (coral, the breaking drift). Every svg id is prefixed "v5-guardrails-4-".
 */

const C = {
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  navLabel: "#62748e",
  accent: "#5eead4",
  firing: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-guardrails-4-";

// Evenly spaced registration ticks in the in-sync zone: the locked contract,
// the secondary visual rhyme. They stop at the schema-change boundary.
const TICKS = [44, 58, 72, 86, 100, 114, 128] as const;

export function GuardrailsVariant4({ className }: GuardrailsVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          client build drift
        </p>

        {/* Two rails: a grey expected baseline and the client thread that drifts. */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="Generated client type tracking the schema in sync, then drifting from the expected int to double? after Product.rating is retyped Int! to Float, caught as a CS0266 compile error."
          className="mt-4"
          style={{ display: "block", fontFamily: C.mono }}
        >
          <defs>
            <marker
              id={`${ID}drift`}
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
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
            </marker>
          </defs>

          {/* Grey expected baseline: the `int` contract the consumer code holds. */}
          <line
            x1="20"
            y1="68"
            x2="258"
            y2="68"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* Registration ticks: the contract locked in sync, ending at the change. */}
          {TICKS.map((x) => (
            <line
              key={`${ID}tick-${x}`}
              x1={x}
              y1="68"
              x2={x}
              y2="73"
              stroke={C.inkFaint}
              strokeWidth="1"
              strokeLinecap="round"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          {/* Schema-change boundary: Product.rating retyped Int! -> Float. */}
          <line
            x1="150"
            y1="52"
            x2="150"
            y2="106"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeDasharray="2 3"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* Teal thread: the regenerated client, coincident with the baseline. */}
          <path
            d="M30 68 H150"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* Coral drift: the generated type peels off after the schema change. */}
          <path
            d="M150 68 L216 104"
            fill="none"
            stroke={C.firing}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}drift)`}
          />

          {/* Measured drift gap: coral dimension bracket between the two rails. */}
          <path
            d="M219 76 H225 M222 76 V98 M219 98 H225"
            fill="none"
            stroke={C.firing}
            strokeWidth="1"
            strokeLinecap="round"
            strokeLinejoin="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* Hollow teal source ring: the regenerated client lineage. */}
          <circle
            cx="24"
            cy="68"
            r="5"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* Grey expected slot: the `int` the assignment still wants. */}
          <circle
            cx="222"
            cy="68"
            r="4"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* Coral focal node: the drift caught as a compile error. */}
          <circle
            cx="222"
            cy="106"
            r="6"
            fill="none"
            stroke={C.firing}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="222" cy="106" r="2.5" fill={C.firing} />

          {/* Sparse micro-labels: the cause, the two types, and the caught error. */}
          <text
            x="150"
            y="46"
            textAnchor="middle"
            fontSize="7"
            letterSpacing="0.06em"
            fill={C.navLabel}
          >
            Int! -&gt; Float
          </text>
          <text x="232" y="71" fontSize="8" fill={C.ink}>
            int
          </text>
          <text x="232" y="109" fontSize="8" fill={C.ink}>
            double?
          </text>
          <text
            x="222"
            y="128"
            textAnchor="middle"
            fontSize="8"
            fill={C.firing}
          >
            CS0266
          </text>
        </svg>

        {/* Single dim caption: the release-safety payoff. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="text-cc-ink-dim text-xs">
            Caught at compile time, not in prod.
          </p>
        </div>
      </div>
    </div>
  );
}
