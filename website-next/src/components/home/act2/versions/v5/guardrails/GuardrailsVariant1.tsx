interface GuardrailsVariant1Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, v5 "Schematic Lines", concept #1: schema diff
 * classified.
 *
 * A reductive monoline diff column. A grey 1px spine carries three open change
 * nodes, one per edited line of `schema.graphql`: `+ reviewCount` and
 * `+ price` are additive (grey nodes, SAFE), while `- legacySku` is a removed
 * field. Each row is a +/- glyph, the field name, and its risk classification,
 * so every change is graded, not just diffed.
 *
 * The one status hue is coral on the breaking row: its node, its `-` glyph,
 * its field name, and its BREAKING chip all turn coral (#f0786a), the genuine
 * risk the registry caught. The single teal thread is the registry-bot resolve
 * pinned to that row: it leaves the hollow teal source ring (registry-bot) and
 * terminates on the teal focal pin (open ring + solid teal dot, teal chevron)
 * sitting on the breaking line. Strip teal and coral and the rest reads as a
 * quiet grey diff; every other node, glyph, and label stays cc-ink-faint grey.
 *
 * cc-* palette only; every stroke is 1px non-scaling. React Server Component,
 * settled final frame, no motion, no hooks. Every svg id is prefixed
 * "v5-guardrails-1-".
 */

const C = {
  surface: "#0c1322",
  ink: "#a1a3af",
  navLabel: "#62748e",
  inkFaint: "rgba(245,241,234,0.16)",
  accent: "#5eead4",
  firing: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v5-guardrails-1-";

// One row per changed line of the diff. The removed field is the breaking
// change and carries the coral status hue; the additive fields stay grey/SAFE.
const ROWS: readonly {
  readonly y: number;
  readonly glyph: string;
  readonly name: string;
  readonly chip: string;
  readonly breaking: boolean;
}[] = [
  { y: 44, glyph: "+", name: "reviewCount", chip: "SAFE", breaking: false },
  { y: 78, glyph: "-", name: "legacySku", chip: "BREAKING", breaking: true },
  { y: 112, glyph: "+", name: "price", chip: "SAFE", breaking: false },
];

export function GuardrailsVariant1({ className }: GuardrailsVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          schema diff
        </p>

        {/* Monoline diff column floating directly on the card, no inner panel. */}
        <svg
          viewBox="0 0 280 150"
          width="100%"
          role="img"
          aria-label="A schema diff of three changed lines, one removed field flagged breaking with a registry-bot resolve thread pinned to it"
          className="mt-4"
          style={{ display: "block", overflow: "visible", fontFamily: C.mono }}
        >
          <defs>
            <marker
              id={`${ID}arrow-teal`}
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
                strokeLinecap="round"
                strokeLinejoin="round"
                vectorEffect="non-scaling-stroke"
              />
            </marker>
          </defs>

          {/* Diff spine: the grey 1px column the change nodes register on. */}
          <line
            x1="24"
            y1="32"
            x2="24"
            y2="124"
            stroke={C.inkFaint}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />

          {/* Teal resolve thread: registry-bot -> pinned focal node on the
              breaking line. Drawn before the pin so the pin sits on top. */}
          <line
            x1="231"
            y1="78"
            x2="204"
            y2="78"
            stroke={C.accent}
            strokeWidth="1"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
            markerEnd={`url(#${ID}arrow-teal)`}
          />

          {/* Change nodes on the spine (breaking node is coral), each occludes
              the spine behind it. */}
          {ROWS.map((row) => (
            <circle
              key={`${ID}node-${row.name}`}
              cx="24"
              cy={row.y}
              r="4"
              fill={C.surface}
              stroke={row.breaking ? C.firing : C.inkFaint}
              strokeWidth="1"
              vectorEffect="non-scaling-stroke"
            />
          ))}

          {/* +/- glyphs. */}
          {ROWS.map((row) => (
            <text
              key={`${ID}glyph-${row.name}`}
              x="40"
              y={row.y + 4}
              textAnchor="middle"
              fontSize="11"
              fill={row.breaking ? C.firing : C.navLabel}
            >
              {row.glyph}
            </text>
          ))}

          {/* Field names. */}
          {ROWS.map((row) => (
            <text
              key={`${ID}name-${row.name}`}
              x="52"
              y={row.y + 3}
              fontSize="9"
              fill={row.breaking ? C.firing : C.ink}
            >
              {row.name}
            </text>
          ))}

          {/* Risk classification chips. */}
          {ROWS.map((row) => (
            <text
              key={`${ID}chip-${row.name}`}
              x="148"
              y={row.y + 2.5}
              fontSize="7"
              letterSpacing="0.08em"
              fill={row.breaking ? C.firing : C.navLabel}
            >
              {row.chip}
            </text>
          ))}

          {/* Teal focal pin: the resolve thread's terminus on the breaking line. */}
          <circle
            cx="197"
            cy="78"
            r="4"
            fill={C.surface}
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <circle cx="197" cy="78" r="2" fill={C.accent} />

          {/* Hollow teal source ring: the registry bot that pins the resolve. */}
          <circle
            cx="242"
            cy="78"
            r="11"
            fill="none"
            stroke={C.accent}
            strokeWidth="1"
            vectorEffect="non-scaling-stroke"
          />
          <text
            x="242"
            y="100"
            textAnchor="middle"
            fontSize="7"
            letterSpacing="0.08em"
            fill={C.navLabel}
          >
            registry-bot
          </text>
        </svg>

        {/* Single-element footer: the changed-line count, one breaking. */}
        <div className="border-cc-card-border mt-4 border-t pt-4">
          <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
            3
          </p>
          <p className="text-cc-ink-dim mt-1.5 text-xs">
            lines changed, 1 breaking
          </p>
        </div>
      </div>
    </div>
  );
}
