interface BuildVariant3Props {
  readonly className?: string;
}

/**
 * "Build loop" scene, v4 "Generated Artifacts", variant 3: the `dotnet build`
 * source-gen pass (locked v4 PATTERN E, a one-line terminal tile + result).
 *
 * One cc-surface terminal tile: a title bar (the Catalog.Api build target + a
 * `build` kind tag) closed by a 1px divider, then the settled stdout of a single
 * `$ dotnet build`. Three ordered emit lines borrowed verbatim from the v1 sibling
 * show the source assembly producing each artifact (`Catalog.Api -> schema.graphql`,
 * `-> Catalog.Resolvers.g.cs`, `-> ProductByIdDataLoader.g.cs`), closed by a healthy
 * green "Build succeeded" check (the one orthogonal status hue, owning real build
 * state, on a different element than the teal token).
 *
 * The single teal callout names the load-bearing artifact: the emitted
 * `schema.graphql` is the one recolored token, with a 3px teal anchor dot, a 2px
 * teal underline tick, a 1px teal leader, and a "GENERATED" micro-label. Strip the
 * teal and the stdout reads as neutral monochrome mono. A Stat duo footer keeps the
 * 0.4s generator emit cost and the 1.8s whole-build total on separate numerals so
 * the two are never conflated. RSC, settled final frame, no motion. Every svg id is
 * prefixed "v4-build-3-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  accent: "#5eead4",
  healthy: "#34d399",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v4-build-3-";

// The three ordered source-generator emits (assembly -> artifact), verbatim from
// the v1 sibling. The schema is the load-bearing token and carries the callout.
const EMITS: readonly { readonly out: string; readonly accent: boolean }[] = [
  { out: "schema.graphql", accent: true },
  { out: "Catalog.Resolvers.g.cs", accent: false },
  { out: "ProductByIdDataLoader.g.cs", accent: false },
];

export function BuildVariant3({ className }: BuildVariant3Props) {
  // Token geometry for the teal callout (monospace advance ~ 4.8px at 8px).
  const charW = 4.8;
  const emitTop = 58;
  const emitStep = 14;
  const tokenX = 14 + "Catalog.Api -> ".length * charW;
  const tokenW = "schema.graphql".length * charW;
  const tokenEnd = tokenX + tokenW;
  const tickY = emitTop + 4;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          build output
        </p>

        <div className="mt-4">
          <svg viewBox="0 0 296 124" width="100%" style={{ display: "block" }}>
            <defs>
              {/* Teal open arrowhead for the single callout leader. */}
              <marker
                id={`${ID}head`}
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

            {/* ---- Terminal tile: the settled dotnet build stdout ---- */}
            <rect
              x="6"
              y="4"
              width="222"
              height="116"
              rx="8"
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* title bar: build target + kind tag */}
            <text x="14" y="16" fontFamily={C.mono} fontSize="9" fill={C.inkDim}>
              Catalog.Api
            </text>
            <text
              x="222"
              y="16"
              textAnchor="end"
              fontFamily={C.mono}
              fontSize="8"
              fill={C.navLabel}
            >
              build
            </text>
            <line
              x1="6"
              y1="22"
              x2="228"
              y2="22"
              stroke={C.cardBorder}
              strokeWidth="1"
            />

            {/* command: a single dotnet build invocation */}
            <text x="14" y="37" fontFamily={C.mono} fontSize="8.5">
              <tspan fill={C.navLabel}>{"$ "}</tspan>
              <tspan fill={C.ink}>dotnet build</tspan>
            </text>

            {/* ordered source-generator emit lines (assembly -> artifact) */}
            {EMITS.map((e, i) => (
              <text
                key={`${ID}emit-${i}`}
                x="14"
                y={emitTop + i * emitStep}
                fontFamily={C.mono}
                fontSize="8"
              >
                <tspan fill={C.inkDim}>Catalog.Api</tspan>
                <tspan fill={C.navLabel}>{" -> "}</tspan>
                <tspan fill={e.accent ? C.accent : C.ink}>{e.out}</tspan>
              </text>
            ))}

            {/* healthy build-succeeded result (the one status hue) */}
            <path
              d="M14 104 L17 107 L22 101"
              fill="none"
              stroke={C.healthy}
              strokeWidth="1.6"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <text x="28" y="108" fontFamily={C.mono} fontSize="8.5" fill={C.ink}>
              Build succeeded
            </text>

            {/* ---- Single teal callout: dot -> underline -> leader -> GENERATED ---- */}
            <circle cx={tokenX - 2} cy={emitTop - 2.5} r="2" fill={C.accent} />
            <line
              x1={tokenX}
              y1={tickY}
              x2={tokenEnd}
              y2={tickY}
              stroke={C.accent}
              strokeWidth="2"
            />
            <path
              d={`M${tokenEnd} ${emitTop + 3} C ${tokenEnd + 32} ${emitTop + 3}, 210 51, 232 50`}
              fill="none"
              stroke={C.accent}
              strokeWidth="1"
              markerEnd={`url(#${ID}head)`}
            />
            <text
              x="236"
              y="53"
              fontFamily={C.mono}
              fontSize="7"
              letterSpacing="0.14em"
              fill={C.accent}
            >
              GENERATED
            </text>
          </svg>
        </div>

        {/* Stat duo footer: generator emit cost kept separate from build total. */}
        <div className="border-cc-card-border mt-4 grid grid-cols-2 gap-4 border-t pt-4">
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              0.4s
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">schema emit</p>
          </div>
          <div>
            <p className="font-heading text-cc-heading text-h4 leading-none font-semibold">
              1.8s
            </p>
            <p className="text-cc-ink-dim mt-1.5 text-xs">build total</p>
          </div>
        </div>
      </div>
    </div>
  );
}
