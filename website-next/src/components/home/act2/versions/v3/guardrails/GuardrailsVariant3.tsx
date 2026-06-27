/**
 * "Release safety" scene, concept 3 ("Published-client impact"), v3 "Signal &
 * Metrics".
 *
 * Archetype B (RANKED-STACK with the bar-meter idiom), so this cell reads as a
 * per-client readiness meter and stays distinct from its verdict-row neighbor
 * (guardrails/2). The honest headline metric is the blast radius: of the published
 * clients the change is reconciled against, how many are no longer clear. The hero
 * is a large cream "2" over the lowercase mono caption "of 3 clients affected".
 * Below it, the three registered clients stack as readiness bars (operations still
 * validating / total), the affected pair drawn in coral and the clear client left
 * faint grey. The lone teal accent is a single reconciliation sweep beneath the
 * stack - "3 of 3 reconciled" - the measurement that backs the number; teal never
 * encodes pass or fail. A dashed-divider footnote names who must migrate.
 *
 * Coral #f0786a is the one status hue and owns only the not-safe state (iOS still
 * at risk, Partner queued); the clear Web client stays neutral and the hero numeral
 * stays cream. Content is faithful to the v2 GuardrailsVariant3 client-impact
 * widget: change checkout-v3 reconciled against Web (5/5 validated), iOS (3/5, at
 * risk), and Partner (0/4, queued).
 *
 * Static settled frame: no animation, no motion, no hooks, no "use client".
 * Server component. Local cc palette object with exact cc-* hex; any SVG id would
 * be prefixed "v3-guardrails-3-" (this cell renders with markup only).
 */

interface GuardrailsVariant3Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the reconciliation sweep (the measurement); coral
 * is the single status hue, rationed to the two clients that are not clear. */
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

type ClientStatus = "ok" | "at-risk" | "queued";

interface ClientRow {
  readonly name: string;
  /** Persisted operations that still validate against the proposed schema. */
  readonly validate: number;
  /** Total persisted operations registered for this client. */
  readonly total: number;
  readonly status: ClientStatus;
}

/* The three published clients checkout-v3 is reconciled against. Web validates
 * cleanly; iOS still sends fields the change removes (at risk); Partner has not
 * cleared any operation yet (queued). The two not-clear clients lead so the coral
 * status reads first. */
const CLIENTS: readonly ClientRow[] = [
  { name: "iOS", validate: 3, total: 5, status: "at-risk" },
  { name: "Partner", validate: 0, total: 4, status: "queued" },
  { name: "Web", validate: 5, total: 5, status: "ok" },
];

export function GuardrailsVariant3({ className }: GuardrailsVariant3Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow + the change under review */}
        <div className="flex items-baseline justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            published-client impact
          </p>
          <span
            className="shrink-0"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.55rem",
              color: cc.navLabel,
            }}
          >
            checkout-v3
          </span>
        </div>

        {/* hero: how many published clients the change leaves not clear */}
        <div className="mt-3.5">
          <p
            className="text-cc-heading leading-none font-semibold"
            style={{ fontFamily: cc.display, fontSize: "2.75rem" }}
          >
            2
          </p>
          <p
            className="text-cc-ink-dim mt-1.5 lowercase"
            style={{ fontFamily: cc.mono, fontSize: "0.62rem" }}
          >
            of 3 clients affected
          </p>
        </div>

        {/* per-client readiness bars: ops still validating / total, coral on the
            two not-clear clients, faint ink on the clear one */}
        <div className="mt-4 space-y-2">
          {CLIENTS.map((row) => (
            <ReadinessBar key={row.name} row={row} />
          ))}
        </div>

        {/* the one teal measurement: the change was reconciled against every client */}
        <div className="mt-3.5 flex items-center gap-2.5">
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
            3 of 3 reconciled
          </span>
        </div>

        {/* interpretation footnote under a dashed divider: who must migrate */}
        <div className="border-cc-ink-faint mt-3.5 border-t border-dashed pt-3">
          <p
            className="flex items-center gap-1.5"
            style={{ fontFamily: cc.mono, fontSize: "0.62rem", color: cc.ink }}
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
            <span style={{ color: cc.heading }}>iOS, Partner</span>
            <span>must migrate before release</span>
          </p>
        </div>
      </div>
    </div>
  );
}

/* One client as a readiness bar: name, the n/total tally, and a track that fills
 * to the share of operations still validating. Not-clear clients (at-risk, queued)
 * take the coral status fill; the clear client is faint ink context. The queued
 * client has validated nothing yet, so its empty well shows a dashed coral edge. */
function ReadinessBar({ row }: { readonly row: ClientRow }) {
  const affected = row.status !== "ok";
  const fraction = row.total === 0 ? 0 : row.validate / row.total;
  const width = Math.round(fraction * 100);
  const queued = row.status === "queued";

  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: "52px 1fr 30px",
        alignItems: "center",
        columnGap: 10,
      }}
    >
      <span
        className="overflow-hidden whitespace-nowrap"
        style={{
          fontFamily: cc.mono,
          fontSize: "0.58rem",
          color: affected ? cc.heading : cc.ink,
          fontWeight: affected ? 600 : 400,
          textOverflow: "ellipsis",
        }}
      >
        {row.name}
      </span>

      <span
        className="relative block w-full rounded-full"
        style={{
          height: 8,
          background: cc.surface,
          border: `1px solid ${cc.cardBorder}`,
        }}
      >
        {queued ? (
          <span
            className="absolute inset-0 rounded-full"
            style={{ border: `1px dashed ${cc.coral}`, opacity: 0.5 }}
          />
        ) : (
          <span
            className="absolute inset-y-0 left-0 rounded-full"
            style={{
              width: `${width}%`,
              background: affected ? cc.coral : cc.ink,
              opacity: affected ? 0.78 : 0.3,
            }}
          />
        )}
      </span>

      <span
        className="tabular-nums"
        style={{
          fontFamily: cc.mono,
          fontSize: "0.6rem",
          textAlign: "right",
          color: affected ? cc.coral : cc.navLabel,
          fontWeight: affected ? 600 : 400,
        }}
      >
        {row.validate}/{row.total}
      </span>
    </div>
  );
}
