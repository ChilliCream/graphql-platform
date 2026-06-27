interface BuildVariant4Props {
  readonly className?: string;
}

/**
 * "Build loop" scene, concept #4 ("Build feedback before runtime"), v3
 * "Signal & Metrics" (strict cc-* dark on a single floating card).
 *
 * Leads with the measured result: `dotnet build` runs the Hot Chocolate source
 * generator, emits the schema and the typed surface, and the compile settles
 * green BEFORE anything runs, so ZERO schema errors escape to runtime. The hero
 * is a lone cream "0" over a lowercase mono caption. The single teal signal is a
 * build-to-runtime gate lane: the build region reads as completed context (a
 * grey-filled h-2 pill), a 1px teal gate line carrying exactly one filled teal
 * node marks the build/runtime boundary, and the runtime pill stays EMPTY because
 * nothing crossed it. The lone status hue is healthy green on the genuine
 * "build passed" tag, since the compile really does succeed; teal still owns the
 * one decorative signal.
 *
 * Content is faithful to the v2 BuildVariant4: the `[QueryType] ProductApi`
 * source declaration that the generator turns into the schema plus the typed
 * client at build time, with build-before-runtime feedback. Layout B (hero figure
 * on top, full-width signal row below); signal family is the gated lane, rotated
 * away from the adjacent build/3 sparkline.
 *
 * Static settled final frame: server component, no animation, no motion, no
 * hooks, no "use client". Strict cc-* dark palette mirrored locally for inline
 * SVG hex. Any svg id would be prefixed "v3-build-4-"; none are required here.
 */

/* Strict cc-* dark palette mirrored for inline SVG use. Teal is the single
 * decorative accent (bound to the build gate); healthy green is the lone status
 * hue (a genuinely passing build). */
const cc = {
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  heading: "#f5f0ea",
  accent: "#5eead4",
  healthy: "#34d399",
} as const;

export function BuildVariant4({ className }: BuildVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow: names the view, identical placement across the set */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          build before runtime
        </p>

        {/* HERO (layout B): the lone cream figure, the measured result */}
        <div className="mt-3 flex items-baseline">
          <span
            className="text-cc-heading font-heading leading-none font-semibold"
            style={{ fontSize: "2.75rem", fontVariantNumeric: "tabular-nums" }}
          >
            0
          </span>
        </div>
        <p className="text-cc-ink-dim mt-1.5 font-mono text-[0.7rem] lowercase">
          schema errors at runtime
        </p>

        {/* the single teal signal: a build-to-runtime gate lane. The build region
            is completed context (grey fill), the 1px teal gate line carries one
            teal node at the boundary, and the runtime lane stays empty because
            zero schema errors crossed the gate. */}
        <div className="mt-4">
          <svg
            viewBox="0 0 280 36"
            width="100%"
            role="img"
            aria-label="The build stage completes and is verified; the build-to-runtime gate lets zero schema errors through, so the runtime lane stays empty."
            style={{ display: "block" }}
          >
            {/* build lane: completed build work, grey context fill on a pill */}
            <rect x={1} y={14} width={158} height={8} rx={4} fill={cc.surface} />
            <rect
              x={1}
              y={14}
              width={158}
              height={8}
              rx={4}
              fill={cc.heading}
              opacity={0.3}
            />
            <rect
              x={1}
              y={14}
              width={158}
              height={8}
              rx={4}
              fill="none"
              stroke={cc.cardBorder}
              strokeWidth={1}
              vectorEffect="non-scaling-stroke"
            />

            {/* the build/runtime gate: one 1px teal signal + one filled teal node */}
            <line
              x1={170}
              y1={5}
              x2={170}
              y2={31}
              stroke={cc.accent}
              strokeWidth={1}
              vectorEffect="non-scaling-stroke"
            />
            <circle cx={170} cy={18} r={3} fill={cc.accent} />

            {/* runtime lane: empty, nothing escaped the gate */}
            <rect
              x={181}
              y={14}
              width={98}
              height={8}
              rx={4}
              fill={cc.surface}
              stroke={cc.cardBorder}
              strokeWidth={1}
              vectorEffect="non-scaling-stroke"
            />
          </svg>

          {/* region labels: build sits under the filled lane, runtime under the
              empty one */}
          <div className="mt-2 flex items-center justify-between">
            <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.08em] uppercase">
              build
            </span>
            <span className="text-cc-nav-label font-mono text-[0.55rem] tracking-[0.08em] uppercase">
              runtime
            </span>
          </div>
        </div>

        {/* footer: what the build emitted + the one genuine status hue (a passing
            compile owns the healthy-green tag; teal owns the metric signal) */}
        <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-3.5">
          <span className="text-cc-ink font-mono text-[0.62rem]">
            schema + types emitted
          </span>
          <span
            className="inline-flex items-center gap-1.5 rounded-full font-mono text-[0.5rem] tracking-[0.08em] uppercase"
            style={{
              border: `1px solid ${cc.healthy}`,
              padding: "2px 8px",
              color: cc.healthy,
            }}
          >
            <svg
              viewBox="0 0 12 12"
              width="9"
              height="9"
              aria-hidden="true"
              style={{ display: "block" }}
            >
              <path
                d="M2.5 6.2 L5 8.6 L9.5 3.4"
                fill="none"
                stroke={cc.healthy}
                strokeWidth="1.4"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
            build passed
          </span>
        </div>
      </div>
    </div>
  );
}
