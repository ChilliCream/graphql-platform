interface BuildVariant4Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration, v4 "Generated Artifacts" take on concept #4,
 * "build feedback before runtime".
 *
 * Locked v4 PATTERN A (one hero artifact tile + a single teal callout). The
 * artifact is a build-time diagnostic on cc-surface: a `ProductResolvers.cs`
 * editor tile where a split `[ObjectType<Product>]` class is missing the
 * `partial` keyword, a coral squiggle under `class`, and below the divider the
 * matching Hot Chocolate source-generator diagnostic `HC0080` with its verbatim
 * message "A split object type class needs to be a partial class".
 *
 * The single teal callout marks the load-bearing token: the `HC0080` code is the
 * one teal token, with a 3px anchor dot, a 2px underline tick, a 1px leader, and
 * a "BUILD-TIME" micro-label, saying this feedback surfaced at compile time, not
 * at runtime. Coral is the orthogonal status channel for the real error state and
 * sits only on the code squiggle and the ERROR pill, never on the teal token.
 *
 * Diagnostic id (HC0080), message, the `Product` type, and the
 * `[ObjectType<Product>]` API are borrowed from the Hot Chocolate analyzer and
 * the v1 sibling so the artifact is accurate. React Server Component, settled
 * final frame, no animation. Every svg id is prefixed "v4-build-4-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-build-4-";

export function BuildVariant4({ className }: BuildVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          build diagnostic
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 138"
            width="100%"
            role="img"
            aria-label="A C# editor tile flagging a missing partial keyword, with the Hot Chocolate HC0080 build diagnostic surfaced at build time."
            style={{ display: "block" }}
          >
            <defs>
              {/* Teal open arrowhead for the single callout leader. */}
              <marker
                id={`${ID}headTeal`}
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
                />
              </marker>
            </defs>

            {/* ---- Hero tile: the build-time diagnostic artifact ---- */}
            <rect
              x="8"
              y="4"
              width="304"
              height="130"
              rx="8"
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* title bar: filename + C# kind tag, closed by a 1px divider */}
            <text
              x="20"
              y="20"
              fontFamily={C.mono}
              fontSize="9.5"
              fontWeight={600}
              fill={C.inkDim}
            >
              ProductResolvers.cs
            </text>
            <text
              x="300"
              y="20"
              textAnchor="end"
              fontFamily={C.mono}
              fontSize="8"
              fill={C.navLabel}
              letterSpacing="0.05em"
            >
              C#
            </text>
            <line
              x1="8"
              y1="28"
              x2="312"
              y2="28"
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* line-number gutter */}
            <line
              x1="33"
              y1="28"
              x2="33"
              y2="72"
              stroke={C.cardBorder}
              strokeWidth="1"
            />
            <text
              x="22"
              y="48"
              fontFamily={C.mono}
              fontSize="8"
              fill={C.navLabel}
            >
              1
            </text>
            <text
              x="22"
              y="65"
              fontFamily={C.mono}
              fontSize="8"
              fill={C.navLabel}
            >
              2
            </text>

            {/* L1: the split object-type attribute */}
            <text x="40" y="48" fontFamily={C.mono} fontSize="9.5">
              <tspan fill={C.inkDim}>[</tspan>
              <tspan fill={C.ink}>ObjectType</tspan>
              <tspan fill={C.inkDim}>&lt;</tspan>
              <tspan fill={C.ink}>Product</tspan>
              <tspan fill={C.inkDim}>&gt;]</tspan>
            </text>

            {/* L2: the class declaration, missing the partial keyword */}
            <text x="40" y="65" fontFamily={C.mono} fontSize="9.5">
              <tspan fill={C.inkDim}>public </tspan>
              <tspan fill={C.inkDim}>class </tspan>
              <tspan fill={C.ink}>ProductResolvers</tspan>
            </text>

            {/* coral squiggle under `class` (the error location in source) */}
            <path
              d="M80 68 q 1.4 -2 2.8 0 t 2.8 0 t 2.8 0 t 2.8 0 t 2.8 0 t 2.8 0 t 2.8 0 t 2.8 0 t 2.8 0 t 2.8 0"
              fill="none"
              stroke={C.coral}
              strokeWidth="1"
            />

            {/* divider between source and the emitted diagnostic */}
            <line
              x1="20"
              y1="80"
              x2="300"
              y2="80"
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* diagnostic header: the HC0080 code is the teal load-bearing token */}
            <text
              x="24"
              y="102"
              fontFamily={C.mono}
              fontSize="11"
              fontWeight={600}
              fill={C.accent}
              style={{ fontVariantNumeric: "tabular-nums" }}
            >
              HC0080
            </text>

            {/* coral ERROR severity pill (orthogonal status channel) */}
            <rect
              x="250"
              y="91"
              width="50"
              height="15"
              rx="7"
              fill={C.coral}
              fillOpacity="0.1"
              stroke={C.coral}
              strokeWidth="1"
            />
            <text
              x="275"
              y="101.5"
              textAnchor="middle"
              fontFamily={C.mono}
              fontSize="7.5"
              letterSpacing="0.08em"
              fill={C.coral}
            >
              ERROR
            </text>

            {/* the verbatim source-generator message */}
            <text
              x="24"
              y="120"
              fontFamily={C.mono}
              fontSize="8"
              fill={C.inkDim}
            >
              A split object type class needs to be a partial class
            </text>

            {/* ---- The single teal callout: dot + tick + leader + label ---- */}
            <circle cx="70" cy="98" r="2.5" fill={C.accent} />
            <line
              x1="24"
              y1="106"
              x2="66"
              y2="106"
              stroke={C.accent}
              strokeWidth="2"
            />
            <line
              x1="70"
              y1="98"
              x2="148"
              y2="98"
              stroke={C.accent}
              strokeWidth="1"
              markerEnd={`url(#${ID}headTeal)`}
            />
            <text
              x="156"
              y="101"
              fontFamily={C.mono}
              fontSize="7"
              letterSpacing="0.14em"
              fill={C.accent}
            >
              BUILD-TIME
            </text>
          </svg>
        </div>

        {/* Dashed caption footer: the build-vs-runtime payoff in one line. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            the build fails here, never a request in production
          </p>
        </div>
      </div>
    </div>
  );
}
