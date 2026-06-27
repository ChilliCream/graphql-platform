/**
 * "Workflow" scene, concept 1 ("Compile-time wiring manifest"), v3 "Signal &
 * Metrics".
 *
 * Leads with the measured result: Mocha's source generator discovered and wired
 * 14 handlers at build time, so the hero is the cream "14" numeral over the
 * lowercase mono caption "handlers wired at compile time" (layout B, stat-top /
 * signal-row). The single teal signal beneath is a 14-cell segment row, one mark
 * per wired handler, the number itself drawn as the picture. Thirteen marks are
 * wired-and-idle (cc-surface fill, faint grey border); exactly one is the live
 * route, the CreateReview command in flight to its handler, carrying the lone
 * teal tint, teal border, and the single filled teal node.
 *
 * Nothing here is failing, so no status hue appears; teal stays bound to the one
 * in-flight route and the hero numeral stays cream. Content is faithful to the
 * v1/v2 wiring manifest: Mocha codegen fans out to the CreateReview command, its
 * ReviewHandler, the ReviewCreated event, and the PublishSaga, all auto-wired.
 *
 * Static settled frame: no animation, no motion, no hooks, no "use client".
 * Server component. Local cc palette object with exact cc-* hex; any svg id would
 * be prefixed "v3-workflows-1-" (this take uses divs and needs none).
 */

interface WorkflowsVariant1Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the single in-flight route; everything else is
 * cream / grey. No status hue: nothing in the manifest is failing. */
const cc = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  display: '"Josefin Sans", Futura, sans-serif',
} as const;

/* The manifest count is the picture: one segment mark per wired handler. The mark
 * at LIVE_INDEX is the CreateReview command in flight to its handler, the one teal
 * route; the remaining marks stay wired and idle. */
const HANDLERS = 14;
const LIVE_INDEX = 2;
const CELLS: readonly number[] = Array.from({ length: HANDLERS }, (_, i) => i);

export function WorkflowsVariant1({ className }: WorkflowsVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow + the source generator under view */}
        <div className="flex items-baseline justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            wiring manifest
          </p>
          <span
            className="shrink-0"
            style={{
              fontFamily: cc.mono,
              fontSize: "0.55rem",
              color: cc.navLabel,
            }}
          >
            Mocha codegen
          </span>
        </div>

        {/* hero numeral (layout B): how many handlers the generator wired */}
        <div className="mt-3">
          <p
            className="text-cc-heading leading-none font-semibold"
            style={{
              fontFamily: cc.display,
              fontSize: "2.75rem",
              fontVariantNumeric: "tabular-nums",
            }}
          >
            {HANDLERS}
          </p>
          <p
            className="text-cc-ink-dim mt-2 lowercase"
            style={{ fontFamily: cc.mono, fontSize: "0.7rem" }}
          >
            handlers wired at compile time
          </p>
        </div>

        {/* the teal signal: one segment mark per wired handler, the count drawn
            as the picture. Thirteen marks are wired and idle (grey); the mark at
            LIVE_INDEX is the live CreateReview route, the lone teal accent with
            the single filled teal node. */}
        <div className="mt-4 flex items-center gap-1">
          {CELLS.map((i) => {
            const live = i === LIVE_INDEX;
            return (
              <span
                key={`v3-workflows-1-cell-${i}`}
                className="relative flex-1 rounded-[3px] border"
                style={{
                  height: 20,
                  background: live ? `${cc.accent}1f` : cc.surface,
                  borderColor: live ? `${cc.accent}99` : cc.inkFaint,
                }}
              >
                {live ? (
                  <span
                    className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 rounded-full"
                    style={{ width: 5, height: 5, background: cc.accent }}
                  />
                ) : null}
              </span>
            );
          })}
        </div>

        {/* interpretation caption under a dashed divider: the live route + the
            rest of the wired set */}
        <div className="border-cc-ink-faint mt-3.5 border-t border-dashed pt-3">
          <p
            style={{
              fontFamily: cc.mono,
              fontSize: "0.62rem",
              color: cc.inkDim,
            }}
          >
            <span style={{ color: cc.ink }}>CreateReview</span> in flight
            &middot; 13 wired and idle
          </p>
        </div>
      </div>
    </div>
  );
}
