/**
 * Production view, concept 5 - "nitro trace replay", v3 "Signal & Metrics".
 *
 * Archetype A (STAT-LEFT / SIGNAL-RIGHT, sparkline form): debugging starts from
 * evidence, and replaying `nitro trace 4b1c8f2a` rebuilds the resolved span tree
 * from captured telemetry. The honest headline is the slow hop the replay pins,
 * so the hero is a large cream "214 ms" over a mono caption naming the span, with
 * a 1px per-hop latency sparkline to its right. The signal is one teal polyline:
 * four calm hops near the floor, then a single needle at the billing gRPC hop.
 * The lone teal node is the replay playhead resting on that spike (the value the
 * headline names); a faint dashed grey scrub line marks its position. A
 * dashed-divider footnote prints the literal command and the share of the trace.
 *
 * Locked v3 rules: exactly one teal element (the sparkline + its single node);
 * the hero numeral stays cream, never teal; all other geometry is grey
 * (cc-ink-faint baseline, scrub line, node ring). No status hue is used, since
 * "slowest replayed hop" is the lead signal here, not a firing breach. Static
 * settled frame - server component, no animation, no hooks, no "use client".
 * cc-* palette mirrored locally; the sparkline needs no svg defs, so it carries
 * no ids (any added later take the "v3-observe-5-" prefix).
 *
 * Content faithful to v1/v2: trace 4b1c8f2a (EShops checkout); root checkout
 * 231ms; users-svc.GetProfile rest 9ms; catalog.GetCart rest 12ms;
 * billing.Charge grpc 214ms (slow); orders.Insert pg 6ms; 214/231 ~ 93%.
 */

interface ObserveVariant5Props {
  readonly className?: string;
}

/* Strict cc-* dark palette, mirrored locally per the v3 system. Teal is the only
 * decorative hue and is bound to the replayed-latency sparkline and its single
 * playhead node; nothing else is tinted. */
const cc = {
  surface: "#0c1322",
  heading: "#f5f0ea",
  inkDim: "rgba(245,241,234,0.62)",
  cardBorder: "rgba(245,241,234,0.12)",
  inkFaint: "rgba(245,241,234,0.16)",
  navLabel: "#62748e",
  accent: "#5eead4",
  mono: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  display: '"Josefin Sans", Futura, sans-serif',
} as const;

/* Per-hop self-time profile sampled across the replayed span tree: request start,
 * the two fast REST hops, the billing gRPC spike, then the Postgres write. The
 * apex is the billing hop the trace flags - where the playhead rests. */
const SPARK_POINTS = "8,47 46,46 84,45 122,9 160,47";

/* The playhead sits on the billing.Charge spike (the headline value). */
const PLAYHEAD_X = 122;
const PLAYHEAD_Y = 9;

export function ObserveVariant5({ className }: ObserveVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-4 backdrop-blur-sm">
        {/* eyebrow: the replayed view */}
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          trace replay
        </p>

        {/* hero band: slow hop on the left (cream, never teal), replayed-latency
            sparkline on the right with the lone teal playhead node */}
        <div className="mt-3 flex items-center gap-4">
          <div className="shrink-0">
            <div className="flex items-baseline gap-1">
              <span
                className="text-cc-heading leading-none font-semibold"
                style={{ fontFamily: cc.display, fontSize: "2.6rem" }}
              >
                214
              </span>
              <span
                className="text-cc-heading leading-none font-semibold"
                style={{ fontFamily: cc.display, fontSize: "1.05rem" }}
              >
                ms
              </span>
            </div>
            <p
              className="text-cc-ink-dim mt-1.5"
              style={{
                fontFamily: cc.mono,
                fontSize: "0.62rem",
                letterSpacing: "0.04em",
              }}
            >
              billing.Charge gRPC
            </p>
          </div>

          <div className="flex-1">
            <svg
              viewBox="0 0 168 56"
              width="100%"
              height="60"
              preserveAspectRatio="xMidYMid meet"
              fill="none"
              role="img"
              aria-label="Replayed per-hop latency, a single spike at the billing gRPC hop"
            >
              {/* grey floor: the inactive baseline */}
              <line
                x1="8"
                y1="51"
                x2="160"
                y2="51"
                stroke={cc.inkFaint}
                strokeWidth="1"
              />
              {/* dashed scrub line: the playhead position (structural grey) */}
              <line
                x1={PLAYHEAD_X}
                y1="5"
                x2={PLAYHEAD_X}
                y2="52"
                stroke={cc.inkFaint}
                strokeWidth="1"
                strokeDasharray="2 3"
              />
              {/* the one teal signal: replayed per-hop latency */}
              <polyline
                points={SPARK_POINTS}
                stroke={cc.accent}
                strokeWidth="1"
                strokeLinejoin="round"
                strokeLinecap="round"
              />
              {/* the one teal node: replay playhead on the billing spike */}
              <circle
                cx={PLAYHEAD_X}
                cy={PLAYHEAD_Y}
                r="4"
                fill={cc.surface}
                stroke={cc.cardBorder}
                strokeWidth="1"
              />
              <circle
                cx={PLAYHEAD_X}
                cy={PLAYHEAD_Y}
                r="2.5"
                fill={cc.accent}
              />
            </svg>
          </div>
        </div>

        {/* footnote under a dashed divider: the literal command + the hop's share */}
        <div className="border-cc-ink-faint mt-4 flex items-center justify-between gap-3 border-t border-dashed pt-3">
          <span
            style={{
              fontFamily: cc.mono,
              fontSize: "0.6rem",
              color: cc.inkDim,
            }}
          >
            <span style={{ color: cc.navLabel }}>$ </span>
            nitro trace 4b1c8f2a
          </span>
          <span
            style={{
              fontFamily: cc.mono,
              fontSize: "0.6rem",
              color: cc.navLabel,
            }}
          >
            93% of 231ms
          </span>
        </div>
      </div>
    </div>
  );
}
