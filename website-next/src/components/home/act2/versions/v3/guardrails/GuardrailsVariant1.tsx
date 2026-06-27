/**
 * "Release safety" scene, concept 1 ("Schema diff classified"), v3 "Signal &
 * Metrics".
 *
 * Archetype C (DUAL-STAT): the honest headline metric is the risk split of the
 * proposed diff, so the hero is a two-up "3 / 1" - three changed lines, one of
 * them breaking - in cream HEADING numerals split by a hairline divider. Beneath
 * the numerals the three changed `type Product` lines render as a compact segment
 * row, each cell carrying its diff marker and field; SAFE lines stay neutral grey
 * and the one removed field takes the coral BREAKING state. The single teal
 * measure mark is the classification sweep under the row: a 1px teal coverage line
 * spanning all three changes with exactly one filled teal node at its terminus -
 * every change classified, none slip through unreviewed. A dashed-divider footer
 * pins the breaking line to its registry-bot resolve thread.
 *
 * Coral #f0786a is the lone status hue and owns only the breaking state (its cell,
 * caption, and the pinned thread). The hero numerals stay cream and teal stays
 * bound to the coverage reading. Content is faithful to v1/v2: schema.graphql
 * (+2 / -1), reviewCount: Int! (SAFE), legacySku: String (BREAKING), price: Money!
 * (SAFE), and registry-bot flagging legacySku as used by 6 ops.
 *
 * Static settled frame: no animation, no motion, no hooks, no "use client".
 * Server component. Local cc palette object with exact cc-* hex; any SVG id would
 * be prefixed "v3-guardrails-1-" (this cell renders with markup only).
 */

interface GuardrailsVariant1Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the classification sweep (the measure mark);
 * coral encodes the single real breaking state. */
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

interface ChangeCell {
  readonly marker: "+" | "-";
  readonly field: string;
  readonly risk: "SAFE" | "BREAKING";
}

/* The cropped `type Product` hunk: two additive lines land SAFE, the removed
 * field is the single BREAKING change. */
const CHANGES: readonly ChangeCell[] = [
  { marker: "+", field: "reviewCount", risk: "SAFE" },
  { marker: "-", field: "legacySku", risk: "BREAKING" },
  { marker: "+", field: "price", risk: "SAFE" },
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
        {/* eyebrow + the file under review */}
        <div className="flex items-baseline justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            schema diff classified
          </p>
          <span
            className="shrink-0"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.55rem",
              color: cc.navLabel,
            }}
          >
            schema.graphql
          </span>
        </div>

        {/* hero dual-stat: 3 changes, 1 breaking - the honest risk split */}
        <div className="mt-3.5 flex items-end gap-4">
          <div>
            <p
              className="text-cc-heading leading-none font-semibold"
              style={{ fontFamily: cc.display, fontSize: "2rem" }}
            >
              3
            </p>
            <p
              className="text-cc-ink-dim mt-1.5 lowercase"
              style={{ fontFamily: cc.mono, fontSize: "0.62rem" }}
            >
              changes
            </p>
          </div>

          <span
            aria-hidden="true"
            className="mb-1.5 self-stretch"
            style={{ width: 1, background: cc.cardBorder }}
          />

          <div>
            <p
              className="text-cc-heading leading-none font-semibold"
              style={{ fontFamily: cc.display, fontSize: "2rem" }}
            >
              1
            </p>
            <p
              className="mt-1.5 lowercase"
              style={{
                fontFamily: cc.mono,
                fontSize: "0.62rem",
                color: cc.coral,
              }}
            >
              breaking
            </p>
          </div>
        </div>

        {/* the classified change lines as a compact segment row */}
        <div className="mt-4 flex items-stretch gap-1.5">
          {CHANGES.map((cell) => (
            <ClassifyCell key={cell.field} cell={cell} />
          ))}
        </div>

        {/* the one teal measure mark: classification swept all 3 changes (3 of 3) */}
        <div className="mt-2.5 flex items-center gap-2.5">
          <span className="relative block h-1.5 flex-1">
            <span
              className="absolute inset-x-0 top-1/2 -translate-y-1/2"
              style={{ height: 1, background: cc.inkFaint }}
            />
            <span
              className="absolute top-1/2 left-0 -translate-y-1/2"
              style={{ height: 1, width: "100%", background: cc.accent }}
            />
            <span
              aria-hidden="true"
              className="absolute top-1/2 right-0 rounded-full"
              style={{
                width: 6,
                height: 6,
                transform: "translateY(-50%)",
                background: cc.accent,
              }}
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
            3 of 3 classified
          </span>
        </div>

        {/* interpretation caption: the breaking line's pinned registry-bot thread */}
        <div className="border-cc-ink-faint mt-3.5 border-t border-dashed pt-3">
          <p
            className="flex items-center gap-1.5"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.62rem",
              color: cc.inkDim,
            }}
          >
            <span
              aria-hidden="true"
              className="rounded-full"
              style={{
                width: 5,
                height: 5,
                flex: "0 0 auto",
                background: cc.coral,
              }}
            />
            <span style={{ color: cc.coral }}>registry-bot</span>
            <span>legacySku used by 6 ops</span>
            <span style={{ flex: 1 }} />
            <span
              className="rounded"
              style={{
                border: `1px solid ${cc.coral}66`,
                color: cc.coral,
                padding: "0 5px",
                letterSpacing: "0.06em",
              }}
            >
              RESOLVE
            </span>
          </p>
        </div>
      </div>
    </div>
  );
}

/* A single classified change line as a segment cell: diff marker + field, framed
 * on cc-surface. SAFE cells stay cream/grey; the BREAKING cell carries a coral
 * border, marker, and field (the one firing status). */
function ClassifyCell({ cell }: { readonly cell: ChangeCell }) {
  const breaking = cell.risk === "BREAKING";
  return (
    <div
      className="flex min-w-0 flex-1 items-center gap-1.5 rounded-[3px] border px-2 py-1.5"
      style={{
        background: cc.surface,
        borderColor: breaking ? `${cc.coral}59` : cc.cardBorder,
      }}
    >
      <span
        aria-hidden="true"
        className="shrink-0"
        style={{
          fontFamily: cc.mono,
          fontSize: "0.65rem",
          color: breaking ? cc.coral : cc.navLabel,
        }}
      >
        {cell.marker}
      </span>
      <span
        className="overflow-hidden"
        style={{
          fontFamily: cc.mono,
          fontSize: "0.55rem",
          color: breaking ? cc.coral : cc.ink,
          whiteSpace: "nowrap",
          textOverflow: "ellipsis",
        }}
      >
        {cell.field}
      </span>
    </div>
  );
}
