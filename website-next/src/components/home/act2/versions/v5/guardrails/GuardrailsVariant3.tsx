interface GuardrailsVariant3Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, v5 "Schematic Lines", concept #3: published-client
 * impact.
 *
 * A reductive monoline fan-out. One hollow teal source ring is the proposed
 * breaking change `checkout-v3`; it fans out to the three published clients
 * registered against the schema (Web, iOS, Partner). Each client carries a
 * per-client readiness bar whose validated length reads the share of its
 * operations that still pass: Web is solid full (5/5), iOS is solid to 3/5 with
 * the at-risk remainder dashed, Partner is fully dashed (0/4). Geometry carries
 * the OK / at-risk / queued distinction so color stays rationed.
 *
 * The single teal thread is the only decorative accent: it leaves the hollow
 * teal source ring, traces the one route the headline names (the change out to
 * the client that breaks) and terminates on Partner's ring, stroked teal with a
 * teal chevron and a solid teal landing dot. The one status hue, coral, is
 * layered only on Partner, the published client whose every operation breaks:
 * its readiness bar, zero-mark tick, name, and 0/4 tally. Everything else stays
 * cc-ink-faint grey.
 *
 * Content carried verbatim from the v1 / v2 siblings so the gallery caption
 * stays accurate: change `checkout-v3` reconciled against Web 5/5 (validated),
 * iOS 3/5 (at risk), Partner 0/4 (breaking); 1 of 3 clients fully ready.
 *
 * React Server Component: no hooks, no client APIs, settled final frame only.
 * Every svg id is prefixed "v5-guardrails-3-".
 */

const ID = "v5-guardrails-3-";

/** Locked v5 monoline palette: grey schematic ink, teal accent, coral status. */
const C = {
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

// Readiness bar scale: validated fraction maps onto a track of up to MAX_LEN.
const BAR_X = 170;
const BAR_MAX = 244;
const MAX_LEN = BAR_MAX - BAR_X;

type ClientStatus = "ok" | "at-risk" | "breaking";

interface ClientRow {
  readonly name: string;
  readonly y: number;
  /** Share of registered operations that still validate, 0..1. */
  readonly frac: number;
  readonly status: ClientStatus;
}

const ROWS: readonly ClientRow[] = [
  { name: "Web", y: 34, frac: 1, status: "ok" },
  { name: "iOS", y: 75, frac: 0.6, status: "at-risk" },
  { name: "Partner", y: 116, frac: 0, status: "breaking" },
] as const;

export function GuardrailsVariant3({ className }: GuardrailsVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          published-client impact
        </p>

        {/* Fan-out: one proposed change ring to three published-client rings. */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="Proposed change checkout-v3 fanning out to three published clients: Web still validates fully, iOS partially, and Partner breaks entirely with zero of four operations passing."
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
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </marker>
            <marker
              id={`${ID}arrow-teal`}
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

          {/* grey fan connectors: change -> Web, change -> iOS */}
          <path
            d="M53.6 69.7 L112.8 36.9"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}arrow-grey)`}
          />
          <path
            d="M55 75 L112 75"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}arrow-grey)`}
          />

          {/* teal thread: change -> the client that breaks (Partner) */}
          <path
            d="M53.6 80.3 L112.8 113.1"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
            strokeLinecap="round"
            strokeLinejoin="round"
            markerEnd={`url(#${ID}arrow-teal)`}
          />

          {/* hollow teal source ring: the proposed change under test */}
          <circle
            cx="44"
            cy="75"
            r="11"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text x="44" y="100" textAnchor="middle" fontSize="8" fill={C.ink}>
            checkout-v3
          </text>

          {/* grey client rings */}
          <circle
            cx="118"
            cy="34"
            r="6"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle
            cx="118"
            cy="75"
            r="6"
            fill="none"
            stroke={C.inkFaint}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />

          {/* focal client ring (teal) + solid teal landing dot: thread terminus */}
          <circle
            cx="118"
            cy="116"
            r="6"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="118" cy="116" r="2.5" fill={C.accent} />

          {/* per-client readiness bars: solid validated length + dashed remainder */}
          {ROWS.map((row) => {
            const end = BAR_X + row.frac * MAX_LEN;
            const statusTone = row.status === "breaking" ? C.coral : C.inkFaint;
            return (
              <g key={row.name}>
                {row.frac > 0 && (
                  <line
                    x1={BAR_X}
                    y1={row.y}
                    x2={end}
                    y2={row.y}
                    stroke={C.inkFaint}
                    strokeWidth="1"
                    strokeLinecap="round"
                    vectorEffect="non-scaling-stroke"
                  />
                )}
                {row.frac < 1 && (
                  <line
                    x1={end}
                    y1={row.y}
                    x2={BAR_MAX}
                    y2={row.y}
                    stroke={statusTone}
                    strokeWidth="1"
                    strokeLinecap="round"
                    strokeDasharray="2 3"
                    vectorEffect="non-scaling-stroke"
                  />
                )}
                {/* measured mark: the validated boundary tick */}
                <line
                  x1={end}
                  y1={row.y - 3}
                  x2={end}
                  y2={row.y + 3}
                  stroke={statusTone}
                  strokeWidth="1"
                  strokeLinecap="round"
                  vectorEffect="non-scaling-stroke"
                />
                <text
                  x="130"
                  y={row.y + 3}
                  fontSize="8"
                  fill={row.status === "breaking" ? C.coral : C.ink}
                >
                  {row.name}
                </text>
              </g>
            );
          })}

          {/* the breaking client's tally: zero of four operations validate */}
          <text x="248" y="119" fontSize="8" fill={C.coral}>
            0/4
          </text>
        </svg>

        {/* lone footer numeral: how many published clients clear the change */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            1/3
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">clients fully ready</p>
        </div>
      </div>
    </div>
  );
}
