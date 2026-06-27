/**
 * "Release safety" scene, concept 5 ("Schema version timeline"), v3 "Signal &
 * Metrics" (dark cc-* panel).
 *
 * Re-expresses the v2 schema.graphql registry promotion in v3's locked metrics
 * strategy with layout archetype A (stat-left / signal-right): lead with the
 * measurable result. The honest headline is the schema version currently live in
 * the registry, so the hero is a cream "@2274" over a lowercase mono caption
 * "current schema", sitting left. Bound to its right, the one teal accent is the
 * version timeline rail: published versions strung top-down on a 1px rail, the
 * lineage turning teal as it reaches the current head and carrying exactly one
 * filled teal node at @2274 (the reading the headline names). Below the current
 * head the rail breaks into a dashed gate gap and the next candidate @2291 hangs
 * as a violet ghost node: it has not landed, held pending review before it can
 * publish.
 *
 * This concept is a review / version gate, so the lone status hue is governance
 * violet, not coral: violet owns only the held candidate (its ghost node, its
 * "review" tag, and the gate phrase in the footnote). Teal stays the supporting
 * signal and the hero figure stays cream; neither ever carries status. The two
 * older published versions are settled history with no live state to assert, so
 * they stay neutral grey.
 *
 * Content is faithful to the v2 GuardrailsVariant5: the registry tracks
 * schema.graphql; the current published head is eshops@2274, preceded by @2268
 * and @2260; the candidate eshops@2291 is held pending review because
 * Product.rating was removed. Tags are shortened to their @NNNN suffix so the
 * four rail entries fit without clipping; the eyebrow names the file.
 *
 * Static settled frame: server component, no "use client", no hooks, no motion.
 * Local cc-* palette mirrors the cc-* tokens exactly; any svg id would be
 * prefixed "v3-guardrails-5-" (this cell renders with markup only).
 */

interface GuardrailsVariant5Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the current-head lineage segment and its single
 * node; governance violet is the lone status hue, rationed to the held candidate
 * at the review gate. */
const cc = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  violet: "#8b8ff0",
  mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  display: '"Josefin Sans", Futura, sans-serif',
} as const;

type VersionState = "published" | "current" | "pending";

interface Version {
  readonly tag: string;
  readonly state: VersionState;
}

/* The version timeline, oldest first: two settled published versions, the current
 * published head, then the gated candidate held before it can publish. */
const VERSIONS: readonly Version[] = [
  { tag: "@2260", state: "published" },
  { tag: "@2268", state: "published" },
  { tag: "@2274", state: "current" },
  { tag: "@2291", state: "pending" },
];

/* Rail geometry. Each entry occupies ROW_H; the node sits at the row centre, so a
 * connector between rows i and i+1 starts at centre(i) and spans one row. */
const ROW_H = 20;
const NODE_X = 8;

export function GuardrailsVariant5({ className }: GuardrailsVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        {/* eyebrow + the file under registry control */}
        <div className="flex items-baseline justify-between gap-3">
          <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
            schema version history
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

        {/* hero band (archetype A): cream current-head figure left, the version
            timeline rail right, vertically centred against it */}
        <div className="mt-4 flex items-center gap-4">
          {/* the measurable result: the schema version currently published */}
          <div className="shrink-0">
            <p
              className="leading-none font-semibold tabular-nums"
              style={{
                margin: 0,
                fontFamily: cc.display,
                fontSize: "2.25rem",
                color: cc.heading,
              }}
            >
              <span
                style={{
                  fontSize: "1.4rem",
                  fontWeight: 400,
                  color: cc.inkDim,
                }}
              >
                @
              </span>
              2274
            </p>
            <p
              className="mt-1.5 lowercase"
              style={{
                fontFamily: cc.mono,
                fontSize: "0.7rem",
                color: cc.inkDim,
              }}
            >
              current schema
            </p>
          </div>

          {/* the one teal signal: the version timeline rail. The lineage runs grey
              then turns teal into the current head, which carries the single teal
              node; a dashed gate gap drops to the violet ghost candidate. */}
          <div
            className="relative min-w-0 flex-1"
            style={{ height: ROW_H * VERSIONS.length }}
          >
            {/* rail segment, oldest -> @2268: settled history, grey */}
            <span
              aria-hidden="true"
              className="absolute"
              style={{
                left: NODE_X,
                top: ROW_H / 2,
                height: ROW_H,
                width: 1,
                background: cc.inkFaint,
                transform: "translateX(-0.5px)",
              }}
            />
            {/* rail segment into the current head: the teal lineage signal */}
            <span
              aria-hidden="true"
              className="absolute"
              style={{
                left: NODE_X,
                top: ROW_H * 1.5,
                height: ROW_H,
                width: 1,
                background: cc.accent,
                transform: "translateX(-0.5px)",
              }}
            />
            {/* the uncrossed gate: a dashed gap down to the held candidate */}
            <span
              aria-hidden="true"
              className="absolute"
              style={{
                left: NODE_X,
                top: ROW_H * 2.5,
                height: ROW_H - 6,
                width: 0,
                borderLeft: `1px dashed ${cc.inkFaint}`,
                transform: "translateX(-0.5px)",
              }}
            />

            {VERSIONS.map((v) => (
              <div
                key={v.tag}
                className="relative flex items-center"
                style={{ height: ROW_H, gap: 8 }}
              >
                <span
                  className="flex shrink-0 items-center justify-center"
                  style={{ width: 16 }}
                >
                  <TimelineNode state={v.state} />
                </span>
                <span
                  className="tabular-nums"
                  style={{
                    fontFamily: cc.mono,
                    fontSize: "0.66rem",
                    fontWeight: v.state === "current" ? 600 : 400,
                    color:
                      v.state === "current"
                        ? cc.heading
                        : v.state === "pending"
                          ? cc.violet
                          : cc.ink,
                  }}
                >
                  {v.tag}
                </span>
                {v.state === "pending" ? (
                  <>
                    <span style={{ flex: 1 }} />
                    <span
                      className="shrink-0 rounded uppercase"
                      style={{
                        border: `1px solid ${cc.violet}66`,
                        color: cc.violet,
                        fontFamily: cc.mono,
                        fontSize: "0.5rem",
                        letterSpacing: "0.08em",
                        padding: "1px 5px",
                      }}
                    >
                      review
                    </span>
                  </>
                ) : null}
              </div>
            ))}
          </div>
        </div>

        {/* interpretation footnote under a dashed divider: the gate + its cause */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
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
              className="shrink-0 rounded-full"
              style={{ width: 5, height: 5, background: cc.violet }}
            />
            <span style={{ color: cc.violet }}>gated before publish</span>
            <span aria-hidden="true">&middot;</span>
            <span style={{ color: cc.ink }}>Product.rating removed</span>
          </p>
        </div>
      </div>
    </div>
  );
}

/* One version on the timeline rail. Settled history is a small neutral grey dot;
 * the current head is the single filled teal node (a cc-surface ring masks the
 * rail behind it); the held candidate is a dashed violet ghost node, the lone
 * status hue marking the review gate it has not crossed. */
function TimelineNode({ state }: { readonly state: VersionState }) {
  if (state === "current") {
    return (
      <span
        aria-hidden="true"
        className="flex items-center justify-center rounded-full"
        style={{
          width: 13,
          height: 13,
          background: cc.surface,
          border: `1px solid ${cc.cardBorder}`,
        }}
      >
        <span
          className="rounded-full"
          style={{ width: 6, height: 6, background: cc.accent }}
        />
      </span>
    );
  }
  if (state === "pending") {
    return (
      <span
        aria-hidden="true"
        className="rounded-full"
        style={{
          width: 11,
          height: 11,
          border: `1px dashed ${cc.violet}`,
          background: `${cc.violet}1f`,
        }}
      />
    );
  }
  return (
    <span
      aria-hidden="true"
      className="rounded-full"
      style={{ width: 7, height: 7, background: cc.navLabel }}
    />
  );
}
