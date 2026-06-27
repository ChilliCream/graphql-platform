interface GuardrailsVariant4Props {
  readonly className?: string;
}

/**
 * "Release safety" scene, concept #4 ("Generated client build drift") - v3
 * "Signal & Metrics".
 *
 * Leads with the measured result: exactly ONE schema field changed type, and the
 * regenerated Strawberry Shake client no longer compiles, so the drift surfaces
 * as a real C# compiler error at build time, before runtime. The hero is a lone
 * cream "1" over a lowercase mono caption, with the specific change pinned beside
 * it (`Product.rating`, `Int! -> Float`). The single teal signal is the build
 * pipeline as a gated segment bar: `generate` succeeded (the client regenerated
 * cleanly, teal fill), but the `compile` segment never completes because the
 * generated `double?` no longer fits the hand-written `int`. The one status hue
 * is coral, and it owns only the genuine failure: the compile gate cross, the
 * compile label, and the CS0266 tag. The hero numeral stays cream; teal stays
 * bound to the generate measurement.
 *
 * Content is faithful to the build baseline: `dotnet build` of the EShops
 * storefront, `ProductSummary.cs(42,28): error CS0266`, cannot convert `double?`
 * to `int` because `Product.rating` was retyped `Int! -> Float`. Layout B (hero
 * figure on top, full-width signal strip below); signal family (d) gated segment
 * bar with a coral compile mark.
 *
 * React Server Component. Static settled final frame: no animation, no hooks, no
 * handlers, no "use client". Strict cc-* dark palette mirrored locally. Any svg
 * id would be prefixed "v3-guardrails-4-"; none are needed here.
 */

/* Strict cc-* dark palette (mirrors app/globals.css cc-* tokens). Teal is the
 * single decorative accent (bound to the generate step); coral is the lone
 * status hue and encodes only the genuine compile failure. */
const cc = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  coral: "#f0786a",
  mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  display: '"Josefin Sans", Futura, sans-serif',
} as const;

/** Coral failure cross, reused at two sizes for the compile gate and the tag. */
function FailMark({ size }: { readonly size: number }) {
  return (
    <svg
      viewBox="0 0 12 12"
      width={size}
      height={size}
      aria-hidden="true"
      style={{ display: "block", flex: "0 0 auto" }}
    >
      <circle
        cx="6"
        cy="6"
        r="4.7"
        fill="none"
        stroke={cc.coral}
        strokeWidth="1.3"
      />
      <path
        d="M4.2 4.2 L7.8 7.8 M7.8 4.2 L4.2 7.8"
        stroke={cc.coral}
        strokeWidth="1.3"
        strokeLinecap="round"
      />
    </svg>
  );
}

export function GuardrailsVariant4({ className }: GuardrailsVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow + the command under inspection */}
        <div className="flex items-baseline justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            build drift caught
          </p>
          <span
            className="shrink-0"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.55rem",
              color: cc.navLabel,
            }}
          >
            dotnet build
          </span>
        </div>

        {/* HERO: one field retyped, with the specific change pinned beside it */}
        <div className="mt-3 flex items-end justify-between gap-4">
          <div>
            <p
              className="text-cc-heading leading-none font-semibold"
              style={{ fontFamily: cc.display, fontSize: "2.75rem" }}
            >
              1
            </p>
            <p
              className="text-cc-ink-dim mt-1.5 lowercase"
              style={{ fontFamily: cc.mono, fontSize: "0.7rem" }}
            >
              compile error at build
            </p>
          </div>

          <div className="shrink-0 text-right">
            <p
              style={{
                margin: 0,
                fontFamily: cc.mono,
                fontSize: "0.58rem",
                color: cc.navLabel,
              }}
            >
              Product.rating
            </p>
            <p
              className="mt-1"
              style={{
                fontFamily: cc.mono,
                fontSize: "0.72rem",
                color: cc.ink,
                whiteSpace: "nowrap",
              }}
            >
              Int!{" "}
              <span style={{ color: cc.inkFaint }} aria-hidden="true">
                &rarr;
              </span>{" "}
              Float
            </p>
          </div>
        </div>

        {/* the single teal signal: the build pipeline as a gated segment bar.
            generate is filled teal (the client regenerated cleanly); the coral
            cross is the compile gate it never crossed; the compile track stays
            empty because the generated double? no longer fits the int. */}
        <div className="mt-4">
          <div className="flex items-center gap-1.5">
            {/* generate segment: teal fill, the one measured success */}
            <span
              className="h-2 flex-1 overflow-hidden rounded-full"
              style={{
                backgroundColor: cc.surface,
                boxShadow: `inset 0 0 0 1px ${cc.cardBorder}`,
              }}
            >
              <span
                className="block h-full w-full rounded-full"
                style={{ backgroundColor: cc.accent, opacity: 0.78 }}
              />
            </span>

            {/* coral compile gate: the genuine failure point */}
            <span
              className="flex shrink-0 items-center justify-center"
              style={{ width: "22px" }}
            >
              <FailMark size={14} />
            </span>

            {/* compile track: empty, the build never crossed the gate */}
            <span
              className="h-2 flex-1 overflow-hidden rounded-full"
              style={{
                backgroundColor: cc.surface,
                boxShadow: `inset 0 0 0 1px ${cc.cardBorder}`,
              }}
            />
          </div>

          {/* labels row, same proportions so each sits under its region */}
          <div className="mt-2 flex items-center gap-1.5">
            <span
              className="flex-1 uppercase"
              style={{
                fontFamily: cc.mono,
                fontSize: "0.5rem",
                letterSpacing: "0.08em",
                color: cc.navLabel,
              }}
            >
              generate
            </span>
            <span className="shrink-0" style={{ width: "22px" }} />
            <span
              className="flex-1 text-right uppercase"
              style={{
                fontFamily: cc.mono,
                fontSize: "0.5rem",
                letterSpacing: "0.08em",
                color: cc.coral,
              }}
            >
              compile
            </span>
          </div>
        </div>

        {/* footer: the source span + the lone status hue on the real compiler
            error (the generated double? cannot convert to the int it feeds) */}
        <div className="border-cc-card-border mt-4 flex items-center justify-between border-t pt-4">
          <span
            style={{
              fontFamily: cc.mono,
              fontSize: "0.62rem",
              color: cc.inkDim,
            }}
          >
            ProductSummary.cs(42,28)
          </span>
          <span
            className="inline-flex items-center gap-1.5 rounded-full"
            style={{
              border: `1px solid ${cc.coral}66`,
              padding: "2px 8px",
              fontFamily: cc.mono,
              fontSize: "0.5rem",
              letterSpacing: "0.08em",
              textTransform: "uppercase",
              color: cc.coral,
            }}
          >
            <FailMark size={9} />
            error CS0266
          </span>
        </div>
      </div>
    </div>
  );
}
