interface GuardrailsVariant2Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, v5 "Schematic Lines", concept #2: registry check blocks
 * merge.
 *
 * A reductive monoline merge schematic. The proposed change (a hollow teal source
 * ring) enters the one required Nitro registry check, drawn as a horizontal row of
 * four sub-step nodes that each name a distinct facet of the same 3-change set:
 * compose, diff, impact, policy. A second grey branch below is `main`. Both branches
 * converge on a merge gate at the right, drawn BLOCKED.
 *
 * The single teal thread is the change's healthy approach: it leaves the teal source
 * ring and runs through `compose` up to the `diff` facet. There the status takes over
 * (the diff fires BREAKING on the removed `Product.rating` field), so the thread turns
 * coral, drops out of the check, and terminates at the blocked merge gate (coral ring
 * + lowered coral barrier). The remaining facets and the `main` branch stay
 * cc-ink-faint grey. Exactly one teal accent (the approach + source ring) and one
 * status hue (coral, the breaking diff and the gate it shuts).
 *
 * Content borrowed verbatim from the v2 sibling: facets compose / diff / impact /
 * policy, 1 of 3 changes breaking, block-on-break policy, merge blocked. React Server
 * Component: no hooks, no client APIs, settled final frame only. Every svg id is
 * prefixed "v5-guardrails-2-".
 */

const C = {
  ink: "#a1a3af",
  navLabel: "#62748e",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  surface: "#0c1322",
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-guardrails-2-";

// The one required check's four sub-step facets, evenly spaced along the row.
const FACETS: readonly { readonly label: string; readonly cx: number }[] = [
  { label: "COMPOSE", cx: 70 },
  { label: "DIFF", cx: 116 },
  { label: "IMPACT", cx: 162 },
  { label: "POLICY", cx: 208 },
];

export function GuardrailsVariant2({ className }: GuardrailsVariant2Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          registry check blocks merge
        </p>

        {/* merge schematic: the change runs the check, diff fires breaking, and
            the traced thread terminates at a blocked merge gate */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="A proposed change runs a required registry check of four facets (compose, diff, impact, policy); the diff facet fires breaking, so the traced path drops to a blocked merge gate where the main branch converges"
          className="mt-4"
          style={{ display: "block", fontFamily: C.mono }}
        >
          <defs>
            <marker
              id={`${ID}arrow-grey`}
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
                stroke={C.inkFaint}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>
            <marker
              id={`${ID}arrow-coral`}
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
                stroke={C.coral}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>
          </defs>

          {/* grey check sequence: diff -> impact -> policy (the rest of the check) */}
          <path
            d="M121 44 L203 44"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* grey main branch converging up into the merge gate */}
          <path
            d="M29 124 L216 124 L216 108"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}arrow-grey)`}
          />

          {/* teal thread: the change's healthy approach, source ring -> diff facet */}
          <path
            d="M29 44 L111 44"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* coral verdict: the breaking diff drops the path to the blocked gate */}
          <path
            d="M116 49 L116 100 L207 100"
            fill="none"
            stroke={C.coral}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}arrow-coral)`}
          />

          {/* hollow teal source ring: the proposed change entering the check */}
          <circle
            cx="24"
            cy="44"
            r="5"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* grey main-branch ring */}
          <circle
            cx="24"
            cy="124"
            r="5"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* the four facet nodes; occluder fills carry the lines behind them */}
          {FACETS.map((facet) => {
            const breaking = facet.label === "DIFF";
            return (
              <circle
                key={`${ID}node-${facet.label}`}
                cx={facet.cx}
                cy="44"
                r="5"
                fill={facet.cx === 208 ? "none" : C.surface}
                stroke={breaking ? C.coral : C.inkFaint}
                strokeWidth="1"
                vectorEffect="non-scaling-stroke"
              />
            );
          })}

          {/* merge gate, drawn BLOCKED: coral ring + lowered coral barrier */}
          <circle
            cx="216"
            cy="100"
            r="8"
            fill={C.surface}
            stroke={C.coral}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <rect
            x="210"
            y="98.5"
            width="12"
            height="3"
            rx="1.5"
            fill={C.coral}
          />

          {/* sparse key-labels: the four facets (diff coral) + the gate */}
          {FACETS.map((facet) => (
            <text
              key={`${ID}label-${facet.label}`}
              x={facet.cx}
              y="30"
              textAnchor="middle"
              fontSize="7"
              letterSpacing="0.08em"
              fill={facet.label === "DIFF" ? C.coral : C.navLabel}
            >
              {facet.label}
            </text>
          ))}
          <text
            x="216"
            y="86"
            textAnchor="middle"
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.navLabel}
          >
            MERGE
          </text>
        </svg>

        {/* lone footer numeral: the breaking share of the 3-change set */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            1 of 3
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">changes breaking</p>
        </div>
      </div>
    </div>
  );
}
