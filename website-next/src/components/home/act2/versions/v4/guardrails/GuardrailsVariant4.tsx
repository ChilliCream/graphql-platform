interface GuardrailsVariant4Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, v4 "Generated Artifacts", concept #4: generated client
 * build drift, caught as a real C# compiler error (locked v4 PATTERN B, two
 * stacked artifact tiles).
 *
 * The top tile is the regenerated Strawberry Shake client `ProductSummary.cs`,
 * whose generated property is now `public double? Rating` after the server retyped
 * `Product.rating` from `Int!` to `Float`. `double?` is the one load-bearing teal
 * token, marked by the signature callout (anchor dot, 2px underline tick, 1px
 * leader, "GENERATED" micro-label) kept entirely inside this tile.
 *
 * The bottom tile is the `dotnet build` terminal: the `$` prompt, the real
 * `error CS0266` with its `ProductSummary.cs(42,28)` span, the `cannot convert
 * double? to int` detail, and the coral `Build FAILED` verdict. Coral is the
 * single status channel and owns the whole lower tile, so teal (top) and coral
 * (bottom) never compete. A thin neutral chevron in the gap reads the generated
 * client into the build.
 *
 * Literal content (ProductSummary.cs, double?, error CS0266, the 42,28 span,
 * cannot convert double? to int, Build FAILED, the Int! -> Float retype) is
 * borrowed verbatim from the v1 sibling. React Server Component: no "use client",
 * no hooks, no animation, settled final frame. Every svg id is prefixed
 * "v4-guardrails-4-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  accentHover: "#99f6e4",
  coral: "#f0786a",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-guardrails-4-";

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

        {/* regenerated client tile -> dotnet build terminal tile */}
        <svg
          viewBox="0 0 320 196"
          width="100%"
          role="img"
          aria-label="The regenerated ProductSummary.cs client now exposes a generated double? Rating property after the schema retyped Product.rating from Int! to Float, so dotnet build fails with a real error CS0266 that cannot convert double? to int."
          className="mt-4"
          style={{ display: "block", fontFamily: MONO }}
        >
          <defs>
            {/* Teal open chevron for the GENERATED callout leader. */}
            <marker
              id={`${ID}arrow`}
              markerWidth="6"
              markerHeight="6"
              refX="3"
              refY="4.6"
              orient="auto"
              markerUnits="userSpaceOnUse"
            >
              <path
                d="M0.5 0 L3 5 L5.5 0"
                fill="none"
                stroke={C.accent}
                strokeWidth="1"
              />
            </marker>
          </defs>

          {/* ---- Top tile: the regenerated Strawberry Shake client ---- */}
          <rect
            x={8}
            y={2}
            width={304}
            height={80}
            rx={8}
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth={1}
          />
          <text x={18} y={18} fill={C.heading} fontSize={9.5} fontWeight={600}>
            ProductSummary.cs
          </text>
          <text
            x={302}
            y={18}
            textAnchor="end"
            fill={C.navLabel}
            fontSize={8}
            letterSpacing="0.08em"
          >
            generated
          </text>
          <line
            x1={8}
            y1={26}
            x2={312}
            y2={26}
            stroke={C.cardBorder}
            strokeWidth={1}
          />

          {/* generated property: double? is the single teal token */}
          <text x={18} y={45} fontSize={9.5}>
            <tspan fill={C.navLabel}>public </tspan>
            <tspan fill={C.accent}>double?</tspan>
            <tspan fill={C.ink}> Rating</tspan>
            <tspan fill={C.navLabel}>{" { get; }"}</tspan>
          </text>

          {/* signature teal callout, kept inside the top tile */}
          <circle cx={78} cy={34} r={2.5} fill={C.accent} />
          <line
            x1={58}
            y1={50}
            x2={98}
            y2={50}
            stroke={C.accent}
            strokeWidth={2}
            strokeLinecap="round"
          />
          <line
            x1={78}
            y1={52}
            x2={78}
            y2={66}
            stroke={C.accent}
            strokeWidth={1}
            markerEnd={`url(#${ID}arrow)`}
          />
          <text
            x={78}
            y={77}
            textAnchor="middle"
            fill={C.accent}
            fontSize={7}
            letterSpacing="0.1em"
          >
            GENERATED
          </text>

          {/* ---- Neutral flow chevron: the generated client into the build ---- */}
          <line
            x1={160}
            y1={84}
            x2={160}
            y2={91}
            stroke={C.navLabel}
            strokeWidth={1}
          />
          <path
            d="M156 89 L160 93 L164 89"
            fill="none"
            stroke={C.navLabel}
            strokeWidth={1.3}
            strokeLinecap="round"
            strokeLinejoin="round"
          />

          {/* ---- Bottom tile: the dotnet build terminal (coral status) ---- */}
          <rect
            x={8}
            y={98}
            width={304}
            height={96}
            rx={8}
            fill={C.surface}
            stroke={C.cardBorder}
            strokeWidth={1}
          />
          <text x={18} y={114} fill={C.inkDim} fontSize={9} fontWeight={600}>
            EShops.csproj
          </text>
          <text
            x={302}
            y={114}
            textAnchor="end"
            fill={C.navLabel}
            fontSize={8}
            letterSpacing="0.08em"
          >
            dotnet
          </text>
          <line
            x1={8}
            y1={122}
            x2={312}
            y2={122}
            stroke={C.cardBorder}
            strokeWidth={1}
          />

          {/* command line: the $ prompt is the only accentHover glyph */}
          <text x={18} y={140} fontSize={9}>
            <tspan fill={C.accentHover}>$ </tspan>
            <tspan fill={C.ink}>dotnet build</tspan>
          </text>

          {/* the real compiler diagnostic, with its source span */}
          <text x={18} y={157} fontSize={9}>
            <tspan fill={C.navLabel}>ProductSummary.cs</tspan>
            <tspan fill={C.navLabel}>(42,28)</tspan>
            <tspan fill={C.inkDim}>: </tspan>
            <tspan fill={C.coral}>error CS0266</tspan>
          </text>
          <text x={30} y={173} fontSize={9}>
            <tspan fill={C.inkDim}>cannot convert </tspan>
            <tspan fill={C.ink}>double?</tspan>
            <tspan fill={C.inkDim}> to </tspan>
            <tspan fill={C.ink}>int</tspan>
          </text>

          {/* the coral verdict */}
          <g
            stroke={C.coral}
            strokeWidth={1.3}
            strokeLinecap="round"
            fill="none"
          >
            <circle cx={24} cy={186.5} r={4.2} />
            <path d="M22 184.5 L26 188.5 M26 184.5 L22 188.5" />
          </g>
          <text x={34} y={190} fill={C.coral} fontSize={9} fontWeight={600}>
            Build FAILED
          </text>
        </svg>

        {/* Dashed caption: the retype is caught at build, not in production. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            a retyped field (Int! to Float) fails the build, not production
          </p>
        </div>
      </div>
    </div>
  );
}
