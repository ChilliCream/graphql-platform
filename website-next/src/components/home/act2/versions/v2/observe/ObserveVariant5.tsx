/**
 * "Production view" scene, variant 5 - v2 Flow Diagram (locked flow-diagram
 * system, an evolution of the ScrollScenes Chip / Arrow / Stat / bar vocabulary).
 *
 * Concept: nitro trace replay. An engineer runs `nitro trace 4b1c8f2a` and the
 * CLI resolves the checkout request into its span tree, each hop carrying its own
 * self-time. The diagram re-expresses that as a WATERFALL/RANK topology: a source
 * command chip flows into a stack of thin duration bars plotted on a shared
 * 0 -> 231ms axis, so the slow billing gRPC hop reads as the long bar it is.
 *
 * The single traced path is the evidence -> action route the headline names: the
 * billing.Charge gRPC hop is the in-flight item, so its bar, its value, and the
 * derived "next: pin billing.Charge" action node carry the one teal accent. The
 * firing status (the slow hop) is encoded with coral, used only as genuine status
 * and never as decoration. Everything structural stays cream-label / grey-ink.
 *
 * Static by design: no animation, no motion, no hooks. React Server Component.
 * Self-contained; every inline SVG id is prefixed "v2-observe-5-".
 */

interface ObserveVariant5Props {
  readonly className?: string;
}

/* Locked cc-* palette, inline so strokes/fills stay on-brand without utilities. */
const C = {
  page: "#0b0f1a",
  surface: "#0c1322",
  cardBorder: "rgba(245,241,234,0.12)",
  connector: "rgba(245,241,234,0.16)",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245,241,234,0.62)",
  navLabel: "#62748e",
  accent: "#5eead4",
  firing: "#f0786a", // genuine status only: the slow / firing billing gRPC hop
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v2-observe-5-";

/* The settled span tree for `nitro trace 4b1c8f2a` (EShops checkout). Each hop
 * carries its self-time; ms is the proportional value plotted on the shared
 * 0 -> 231ms axis. The billing gRPC hop is the firing span. */
interface Span {
  readonly name: string;
  readonly kind: string;
  readonly label: string; // duration as printed
  readonly ms: number; // numeric self-time for bar plotting
  readonly root?: boolean;
  readonly firing?: boolean; // the slow billing gRPC hop, the traced item
}

const SPANS: readonly Span[] = [
  { name: "checkout", kind: "graphql", label: "231ms", ms: 231, root: true },
  { name: "users-svc.GetProfile", kind: "rest", label: "9ms", ms: 9 },
  { name: "catalog.GetCart", kind: "rest", label: "12ms", ms: 12 },
  {
    name: "billing.Charge",
    kind: "grpc",
    label: "214ms",
    ms: 214,
    firing: true,
  },
  { name: "orders.Insert", kind: "pg", label: "6ms", ms: 6 },
] as const;

export function ObserveVariant5({ className }: ObserveVariant5Props) {
  // Cell geometry: one ~320x176 panel.
  const W = 320;
  const H = 176;

  // Waterfall lane geometry.
  const rowTop = 56; // baseline y of the first span row
  const rowH = 15; // row pitch
  const barX0 = 132; // 0ms axis x (bar origin)
  const barX1 = 300; // 231ms axis x (bar end)
  const barH = 6;
  const maxMs = 231;
  const wAt = (ms: number) => ((barX1 - barX0) * ms) / maxMs;

  // The firing billing hop is index 3; its bar end drives the teal trace marker.
  const billing = SPANS[3];
  const billingRowY = rowTop + 3 * rowH;
  const billingBarEnd = barX0 + wAt(billing.ms);

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <svg
        viewBox={`0 0 ${W} ${H}`}
        width="100%"
        role="img"
        aria-label="Flow diagram of a nitro trace replay: the nitro trace command resolves the checkout span tree into per-hop duration bars on a shared time axis, the slow billing gRPC hop flagged firing and traced to a pin-the-hop next action"
        style={{ display: "block" }}
      >
        <defs>
          {/* Thin open arrowhead for the one traced (teal) connector. */}
          <marker
            id={`${ID}arrow-accent`}
            viewBox="0 0 8 8"
            refX="6.5"
            refY="4"
            markerWidth="7"
            markerHeight="7"
            orient="auto-start-reverse"
          >
            <path
              d="M1,1 L6.5,4 L1,7"
              fill="none"
              stroke={C.accent}
              strokeWidth={1}
            />
          </marker>
          {/* Thin open arrowhead for grey structural connectors. */}
          <marker
            id={`${ID}arrow-grey`}
            viewBox="0 0 8 8"
            refX="6.5"
            refY="4"
            markerWidth="7"
            markerHeight="7"
            orient="auto-start-reverse"
          >
            <path
              d="M1,1 L6.5,4 L1,7"
              fill="none"
              stroke={C.connector}
              strokeWidth={1}
            />
          </marker>
        </defs>

        {/* Outer card: cc-card-bg over cc-page, 1px cc-card-border, rounded-2xl. */}
        <rect x="0" y="0" width={W} height={H} rx="16" fill={C.page} />
        <rect
          x="0.5"
          y="0.5"
          width={W - 1}
          height={H - 1}
          rx="15.5"
          fill="rgba(12,19,34,0.55)"
          stroke={C.cardBorder}
          strokeWidth={1}
        />

        {/* Panel eyebrow (ScrollScenes header). */}
        <text
          x="16"
          y="24"
          fill={C.navLabel}
          fontFamily={MONO}
          fontSize={8.5}
          letterSpacing="0.15em"
          style={{ textTransform: "uppercase" }}
        >
          trace replay
        </text>

        {/* ===== Source command chip -> span tree (the originating node) ===== */}
        {/* The teal active source: `$ nitro trace 4b1c8f2a` begins the traced path. */}
        <g>
          <rect
            x="16"
            y="32"
            width="150"
            height="18"
            rx="8"
            fill={C.surface}
            stroke={C.accent}
            strokeOpacity={0.6}
            strokeWidth={1}
          />
          <text
            x="25"
            y="44.5"
            fontFamily={MONO}
            fontSize={9}
            letterSpacing="0.02em"
          >
            <tspan fill={C.accent} opacity={0.7}>
              ${" "}
            </tspan>
            <tspan fill={C.accent}>nitro trace </tspan>
            <tspan fill={C.accent} opacity={0.85}>
              4b1c8f2a
            </tspan>
          </text>
        </g>

        {/* Right-aligned span count, mono dim. */}
        <text
          x={W - 16}
          y="44.5"
          textAnchor="end"
          fill={C.inkDim}
          fontFamily={MONO}
          fontSize={8.5}
          letterSpacing="0.04em"
        >
          5 spans
        </text>

        {/* shared time axis above the bars: 0 -> 231ms, ticked grey. */}
        <g>
          <line
            x1={barX0}
            y1={rowTop - 8}
            x2={barX1}
            y2={rowTop - 8}
            stroke={C.connector}
            strokeWidth={1}
          />
          {[0, 0.5, 1].map((f, i) => {
            const tx = barX0 + f * (barX1 - barX0);
            const labels = ["0", "115", "231ms"];
            return (
              <g key={`${ID}axis-${i}`}>
                <line
                  x1={tx}
                  y1={rowTop - 11}
                  x2={tx}
                  y2={rowTop - 8}
                  stroke={C.connector}
                  strokeWidth={1}
                />
                <text
                  x={tx}
                  y={rowTop - 13}
                  textAnchor={i === 0 ? "start" : i === 2 ? "end" : "middle"}
                  fill={C.navLabel}
                  fontFamily={MONO}
                  fontSize={7.5}
                  letterSpacing="0.04em"
                  style={{ fontVariantNumeric: "tabular-nums" }}
                >
                  {labels[i]}
                </text>
              </g>
            );
          })}
        </g>

        {/* connector from the source chip down into the span stack (grey). */}
        <path
          d={`M24,50 L24,${rowTop + 1} L${barX0 - 64},${rowTop + 1}`}
          fill="none"
          stroke={C.connector}
          strokeWidth={1}
          markerEnd={`url(#${ID}arrow-grey)`}
        />

        {/* ===== Span rows: name + kind + plotted self-time bar ===== */}
        {SPANS.map((span, index) => {
          const y = rowTop + index * rowH;
          const baseline = y + 4;
          const bw = wAt(span.ms);
          const barY = y;
          const isFiring = span.firing === true;
          // Teal only on the traced (firing billing) hop; cream/grey otherwise.
          const nameFill = isFiring ? C.heading : span.root ? C.heading : C.ink;
          const valueFill = isFiring ? C.firing : C.inkDim;

          return (
            <g key={`${ID}span-${index}`}>
              {/* span name (mono identifier). */}
              <text
                x="16"
                y={baseline}
                fill={nameFill}
                fontFamily={MONO}
                fontSize={9}
                fontWeight={span.root ? 500 : 400}
                letterSpacing="0.01em"
              >
                {span.name}
              </text>
              {/* kind tag, dim, right before the axis origin. */}
              <text
                x={barX0 - 6}
                y={baseline}
                textAnchor="end"
                fill={C.navLabel}
                fontFamily={MONO}
                fontSize={7.5}
                letterSpacing="0.04em"
              >
                {span.kind}
              </text>

              {/* bar track (faint), then the plotted self-time bar. */}
              <rect
                x={barX0}
                y={barY}
                width={barX1 - barX0}
                height={barH}
                rx={barH / 2}
                fill={C.surface}
              />
              <rect
                x={barX0}
                y={barY}
                width={bw}
                height={barH}
                rx={barH / 2}
                fill={isFiring ? C.firing : C.accent}
                opacity={isFiring ? 0.7 : span.root ? 0.32 : 0.45}
              />

              {/* right-aligned printed duration value. */}
              <text
                x={barX1}
                y={baseline}
                textAnchor="end"
                fill={valueFill}
                fontFamily={MONO}
                fontSize={8.5}
                fontWeight={isFiring ? 600 : 400}
                letterSpacing="0.04em"
                style={{ fontVariantNumeric: "tabular-nums" }}
              >
                {span.label}
              </text>
            </g>
          );
        })}

        {/* firing status chip interrupting the traced path at the billing bar. */}
        <g>
          <rect
            x={billingBarEnd + 6}
            y={billingRowY - 1}
            width="34"
            height="11"
            rx="3"
            fill={C.surface}
            stroke={C.firing}
            strokeWidth={1}
          />
          <text
            x={billingBarEnd + 23}
            y={billingRowY + 7}
            textAnchor="middle"
            fill={C.firing}
            fontFamily={MONO}
            fontSize={7}
            letterSpacing="0.08em"
          >
            FIRING
          </text>
        </g>

        {/* ===== Divider + summary stat: 93% of the request is the billing hop. */}
        <line
          x1="16"
          y1="138"
          x2={W - 16}
          y2="138"
          stroke={C.cardBorder}
          strokeWidth={1}
        />

        {/* total stat (cream numeral) on the left. */}
        <text x="16" y="153" fontFamily={MONO} fontSize={8.5}>
          <tspan fill={C.navLabel} letterSpacing="0.04em">
            total{" "}
          </tspan>
          <tspan
            fill={C.heading}
            fontWeight={600}
            style={{ fontVariantNumeric: "tabular-nums" }}
          >
            231ms
          </tspan>
          <tspan fill={C.inkDim}> · </tspan>
          <tspan
            fill={C.firing}
            fontWeight={600}
            style={{ fontVariantNumeric: "tabular-nums" }}
          >
            93%
          </tspan>
          <tspan fill={C.inkDim}> in billing</tspan>
        </text>

        {/* ===== Traced path -> derived next-action node (the one teal route). */}
        {/* Teal connector from the firing billing hop down to the action node. */}
        <path
          d={`M${billingBarEnd},${billingRowY + barH} L${billingBarEnd},150 L210,150 L210,156`}
          fill="none"
          stroke={C.accent}
          strokeWidth={1}
          markerEnd={`url(#${ID}arrow-accent)`}
        />

        {/* derived/terminal action node: rounded-md, tighter, teal-active. */}
        <g>
          <rect
            x="160"
            y="156"
            width="144"
            height="14"
            rx="6"
            fill={C.surface}
            stroke={C.accent}
            strokeOpacity={0.6}
            strokeWidth={1}
          />
          <text
            x="167"
            y="166"
            fontFamily={MONO}
            fontSize={8}
            letterSpacing="0.02em"
          >
            <tspan fill={C.navLabel}>next: </tspan>
            <tspan fill={C.accent}>pin billing.Charge</tspan>
          </text>
        </g>
      </svg>
    </div>
  );
}
