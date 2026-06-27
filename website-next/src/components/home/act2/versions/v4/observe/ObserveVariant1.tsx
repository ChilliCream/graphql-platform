interface ObserveVariant1Props {
  readonly className?: string;
}

/**
 * "Production view" scene, v4 "Generated Artifacts", variant 1: the operation
 * health card caught mid-spike.
 *
 * Locked v4 PATTERN A (one cc-surface artifact tile + a single callout). The
 * tile is the Nitro operation card for `checkout`: a title bar with the status
 * dot, the cream operation name, the `query` span-kind tag, and the amber
 * "INVESTIGATING" status pill. Below it a monochrome cc-ink p99 sparkline kinks
 * sharply up past a dashed SLO threshold line, and a four-up strip reports the
 * headline metrics (P99, P95, ERRORS, IMPACT).
 *
 * Because the subject is a real status event (an operation investigating a
 * latency breach), amber is the single focal cluster and teal steps aside
 * entirely: the amber leading dot and "INVESTIGATING" pill, the amber breach dot
 * at the spike tip, and the signature callout (1px amber leader -> the
 * load-bearing `86 ms` token -> 2px tick -> "SLO BREACH" micro-label) all tell
 * one story. Strip the amber and a neutral grey figure remains. The lone cream
 * strong token is the operation name; every metric value is cc-ink.
 *
 * Literal content (operation `checkout`, span-kind `query`, INVESTIGATING
 * status, p99 86 ms, p95 42 ms, errors 0.3%, impact #1) is borrowed verbatim
 * from the v1 / ScrollScenes siblings. React Server Component, settled final
 * frame, no motion. Every svg id is prefixed "v4-observe-1-".
 */

const CC = {
  surface: "#0c1322",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  inkFaint: "rgba(245,241,234,0.16)",
  cardBorder: "rgba(245,241,234,0.12)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  amber: "#fbbf24",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

// p99 latency sample (ms): a flat baseline that kinks sharply up at the tail,
// the regression the on-call engineer is investigating.
const P99_SERIES = [
  38, 40, 39, 41, 40, 42, 41, 43, 42, 44, 48, 57, 71, 86,
] as const;

// Sparkline band geometry inside the tile (screen-flat).
const BAND_X = 24;
const BAND_W = 200;
const BAND_TOP = 52;
const BAND_H = 36;
const SLO_MS = 50;

function buildSpark() {
  const min = Math.min(...P99_SERIES);
  const max = Math.max(...P99_SERIES);
  const bottom = BAND_TOP + BAND_H;
  const xOf = (i: number) => BAND_X + (i / (P99_SERIES.length - 1)) * BAND_W;
  const yOf = (v: number) => bottom - ((v - min) / (max - min)) * BAND_H;
  const pts = P99_SERIES.map((v, i) => [xOf(i), yOf(v)] as const);
  const line = pts
    .map(([x, y], i) => `${i === 0 ? "M" : "L"}${x.toFixed(1)} ${y.toFixed(1)}`)
    .join(" ");
  return { line, tip: pts[pts.length - 1], sloY: yOf(SLO_MS) };
}

const METRICS: readonly { readonly label: string; readonly value: string }[] = [
  { label: "P99", value: "86 ms" },
  { label: "P95", value: "42 ms" },
  { label: "ERRORS", value: "0.3%" },
  { label: "IMPACT", value: "#1" },
];

export function ObserveVariant1({ className }: ObserveVariant1Props) {
  const spark = buildSpark();
  const [tipX, tipY] = spark.tip;

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          production view
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 154"
            width="100%"
            role="img"
            aria-label="Operation card for checkout, status investigating, p99 latency spiking to 86 ms past the SLO line."
            style={{ display: "block", fontFamily: MONO }}
          >
            {/* Operation tile. */}
            <rect
              x={8}
              y={2}
              width={304}
              height={150}
              rx={8}
              fill={CC.surface}
              stroke={CC.cardBorder}
              strokeWidth={1}
            />

            {/* Title bar: status dot, operation name, span-kind tag. */}
            <circle cx={24} cy={17} r={3} fill={CC.amber} />
            <text
              x={33}
              y={21}
              fill={CC.heading}
              fontSize={11.5}
              fontWeight={600}
            >
              checkout
            </text>
            <rect
              x={96}
              y={11}
              width={34}
              height={15}
              rx={3}
              fill={CC.surface}
              stroke={CC.cardBorder}
              strokeWidth={1}
            />
            <text
              x={113}
              y={21.5}
              fill={CC.navLabel}
              fontSize={8}
              textAnchor="middle"
            >
              query
            </text>

            {/* Amber "Investigating" status pill (real status). */}
            <rect
              x={196}
              y={9}
              width={104}
              height={16}
              rx={8}
              fill={CC.amber}
              fillOpacity={0.08}
              stroke={CC.amber}
              strokeWidth={1}
            />
            <circle cx={206} cy={17} r={2.5} fill={CC.amber} />
            <text
              x={214}
              y={20.5}
              fill={CC.amber}
              fontSize={8}
              letterSpacing="0.04em"
            >
              INVESTIGATING
            </text>

            <line
              x1={8}
              y1={31}
              x2={312}
              y2={31}
              stroke={CC.cardBorder}
              strokeWidth={1}
            />

            {/* p99 latency caption. */}
            <text
              x={24}
              y={45}
              fill={CC.navLabel}
              fontSize={8}
              letterSpacing="0.08em"
            >
              P99 LATENCY
            </text>

            {/* Dashed SLO threshold line + label. */}
            <line
              x1={BAND_X}
              y1={spark.sloY}
              x2={BAND_X + BAND_W}
              y2={spark.sloY}
              stroke={CC.inkFaint}
              strokeWidth={1}
              strokeDasharray="3 3"
            />
            <text
              x={BAND_X + 2}
              y={spark.sloY - 3}
              fill={CC.navLabel}
              fontSize={7}
              letterSpacing="0.06em"
            >
              SLO
            </text>

            {/* Monochrome p99 sparkline kinking up past the SLO line. */}
            <path
              d={spark.line}
              fill="none"
              stroke={CC.ink}
              strokeWidth={1.5}
              strokeLinecap="round"
              strokeLinejoin="round"
            />

            {/* Signature amber callout: breach dot -> leader -> 86 ms token. */}
            <circle
              cx={tipX}
              cy={tipY}
              r={4}
              fill={CC.amber}
              fillOpacity={0.18}
            />
            <circle cx={tipX} cy={tipY} r={2.2} fill={CC.amber} />
            <path
              d={`M${(tipX + 1).toFixed(1)} ${(tipY + 1).toFixed(1)} L240 60`}
              fill="none"
              stroke={CC.amber}
              strokeWidth={1}
            />
            <text x={242} y={64} fill={CC.amber} fontSize={12} fontWeight={600}>
              86 ms
            </text>
            <line
              x1={242}
              y1={68}
              x2={278}
              y2={68}
              stroke={CC.amber}
              strokeWidth={2}
            />
            <text
              x={242}
              y={79}
              fill={CC.amber}
              fontSize={7}
              letterSpacing="0.1em"
            >
              SLO BREACH
            </text>

            {/* Divider above the headline-metrics strip. */}
            <line
              x1={20}
              y1={100}
              x2={300}
              y2={100}
              stroke={CC.cardBorder}
              strokeWidth={1}
            />

            {/* Four-up metric strip: the operation's headline numbers. */}
            {METRICS.map((m, i) => {
              const x = 24 + i * 70;
              return (
                <g key={`v4-observe-1-metric-${m.label}`}>
                  <text
                    x={x}
                    y={117}
                    fill={CC.navLabel}
                    fontSize={7}
                    letterSpacing="0.08em"
                  >
                    {m.label}
                  </text>
                  <text
                    x={x}
                    y={135}
                    fill={CC.ink}
                    fontSize={11}
                    style={{ fontVariantNumeric: "tabular-nums" }}
                  >
                    {m.value}
                  </text>
                </g>
              );
            })}
          </svg>
        </div>

        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            one operation&apos;s health, read from live traffic
          </p>
        </div>
      </div>
    </div>
  );
}
