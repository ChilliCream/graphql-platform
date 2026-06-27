/**
 * "Release safety" scene, concept 2 ("Registry check blocks merge"), v3 "Signal
 * & Metrics".
 *
 * Archetype A (NUMBER-LEAD + STATUS): the honest headline metric is how many of
 * the one required check's four sub-steps come back failing, so the hero is a
 * cream "1 / 4" score over the mono caption "facets failing", paired in the same
 * row with the single coral BLOCKED tag (the merge outcome). Directly under the
 * number, its lone teal measurement is a single coverage bar reading "4 of 4
 * evaluated" - the required check ran every facet and still holds the merge. The
 * four facets of the same 3-change set then list below as compact verdict rows;
 * three are neutral grey context and the diff facet is the one genuine breach,
 * the only coral row. A dashed-divider footnote names the offending change.
 *
 * Teal is the one decorative hue and is bound only to the coverage measurement
 * under the hero; it never encodes pass or fail. Coral is the single status hue
 * and owns only the breaking diff facet, the BLOCKED tag, and the named removal
 * in the footnote. The hero score stays cream. Content is faithful to the v2
 * GuardrailsVariant2: one required registry check fans into compose (3 staged),
 * diff (1 breaking), impact (3 clients), policy (block on break); the breaking
 * change is Product.rating removed, affecting published clients.
 *
 * Static settled frame: no animation, no motion, no hooks, no "use client".
 * Server component. Local cc palette object with exact cc-* hex; any SVG id would
 * be prefixed "v3-guardrails-2-" (this cell renders with markup only).
 */

interface GuardrailsVariant2Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the coverage measurement under the hero; coral
 * encodes the single real breaking state (the diff facet, the blocked merge, the
 * removed field). */
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

interface Facet {
  readonly label: string;
  /** Right-aligned mono note: the facet's verdict on the 3-change set. */
  readonly note: string;
  /** The diff facet is the one that fires breaking and carries the coral state. */
  readonly breaking: boolean;
}

/* The four sub-steps of the one required registry check. Each names a distinct
 * facet of the same 3-change set; only the diff facet fires breaking. */
const FACETS: readonly Facet[] = [
  { label: "compose", note: "3 staged", breaking: false },
  { label: "diff", note: "1 breaking", breaking: true },
  { label: "impact", note: "3 clients", breaking: false },
  { label: "policy", note: "block on break", breaking: false },
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
        {/* eyebrow + required marker */}
        <div className="flex items-baseline justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            registry check blocks merge
          </p>
          <span
            className="shrink-0 uppercase"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.55rem",
              letterSpacing: "0.1em",
              color: cc.navLabel,
            }}
          >
            required
          </span>
        </div>

        {/* hero score + the single status hue: the merge is blocked */}
        <div className="mt-3 flex items-end justify-between gap-3">
          <div>
            <p
              className="text-cc-heading leading-none font-semibold"
              style={{
                fontFamily: cc.display,
                fontSize: "2.5rem",
                fontVariantNumeric: "tabular-nums",
              }}
            >
              1
              <span
                style={{
                  fontSize: "1.25rem",
                  color: cc.inkDim,
                  fontWeight: 400,
                }}
              >
                {" / 4"}
              </span>
            </p>
            <p
              className="mt-1.5 lowercase"
              style={{
                fontFamily: cc.mono,
                fontSize: "0.7rem",
                color: cc.inkDim,
              }}
            >
              facets failing
            </p>
          </div>

          <span
            className="inline-flex items-center gap-1.5 rounded-md border px-2 py-1"
            style={{
              borderColor: `${cc.coral}66`,
              fontFamily: cc.mono,
              fontSize: "0.5rem",
              letterSpacing: "0.08em",
              color: cc.coral,
              textTransform: "uppercase",
            }}
          >
            <span
              className="inline-block h-1.5 w-1.5 rounded-full"
              style={{ background: cc.coral }}
            />
            blocked
          </span>
        </div>

        {/* the one teal measurement: the required check ran every facet (coverage,
            not a verdict) - its visual underline of the hero number */}
        <div className="mt-4 flex items-center gap-2.5">
          <span
            className="relative block h-2 flex-1 overflow-hidden rounded-full"
            style={{
              background: cc.surface,
              border: `1px solid ${cc.cardBorder}`,
            }}
          >
            <span
              className="absolute inset-y-0 left-0 rounded-full"
              style={{ width: "100%", background: cc.accent, opacity: 0.8 }}
            />
          </span>
          <span
            className="shrink-0 tabular-nums"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.55rem",
              letterSpacing: "0.06em",
              color: cc.inkDim,
            }}
          >
            4 of 4 evaluated
          </span>
        </div>

        {/* facet verdicts: the cleared facets stay neutral grey context, the diff
            facet is the single coral breach */}
        <div className="border-cc-card-border mt-4 space-y-2 border-t pt-4">
          {FACETS.map((facet) => (
            <FacetRow key={facet.label} facet={facet} />
          ))}
        </div>

        {/* interpretation footnote: the offending change, named */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p
            className="flex items-center gap-1.5"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.62rem",
              color: cc.inkDim,
            }}
          >
            <span aria-hidden="true" style={{ color: cc.coral }}>
              &minus;
            </span>
            <span style={{ color: cc.ink }}>Product.rating</span>
            <span>removed</span>
            <span style={{ flex: 1 }} />
            <span>merge held</span>
          </p>
        </div>
      </div>
    </div>
  );
}

/* A single facet verdict row: a status mark, the mono facet label, and a right
 * note. Cleared facets carry a faint grey ring and grey text (neutral context);
 * the breaking diff facet carries a solid coral dot and tints its label and note
 * (the one real status). No teal here - teal stays bound to the coverage bar. */
function FacetRow({ facet }: { readonly facet: Facet }) {
  const breaking = facet.breaking;
  return (
    <div className="flex items-center gap-2.5">
      <span
        aria-hidden="true"
        className="block shrink-0 rounded-full"
        style={{
          width: 6,
          height: 6,
          background: breaking ? cc.coral : "transparent",
          border: breaking ? "none" : `1px solid ${cc.navLabel}`,
        }}
      />
      <span
        style={{
          fontFamily: cc.mono,
          fontSize: "0.62rem",
          color: breaking ? cc.coral : cc.ink,
          fontWeight: breaking ? 600 : 400,
        }}
      >
        {facet.label}
      </span>
      <span style={{ flex: 1 }} />
      <span
        className="shrink-0 tabular-nums"
        style={{
          fontFamily: cc.mono,
          fontSize: "0.55rem",
          color: breaking ? cc.coral : cc.inkDim,
          whiteSpace: "nowrap",
        }}
      >
        {facet.note}
      </span>
    </div>
  );
}
