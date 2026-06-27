interface ObserveVariant5Props {
  readonly className?: string;
}

/**
 * "Production view" scene, v4 "Generated Artifacts", variant 5: the span tree the
 * Nitro CLI prints when you replay one request by trace id.
 *
 * Locked v4 PATTERN C (a terminal tile + its settled result). One cc-surface tile
 * is the Nitro CLI: a `nitro` / `cli` title bar, the `$ nitro trace 4b1c8f2a`
 * command, and the resolved span tree it prints. Five hops are borrowed verbatim
 * from the v1 sibling - the `checkout` GraphQL root, the `users-svc` and `catalog`
 * REST hops, the `billing.Charge` gRPC hop, and the `orders.Insert` Postgres
 * write - each carrying its own self-time. A closing summary restates the honest
 * whole-trace total so the single 214ms billing hop is never read as the request
 * total.
 *
 * Status is the subject here (the engineer is chasing the one slow hop), so coral
 * (not teal) owns the single accent cluster, matching how the sibling waterfall
 * treats the same firing hop: the coral `214ms` self-time token, a 2px underline
 * tick, a 2.5px coral anchor dot, a 1px coral leader into the right gutter, and a
 * "SLOW HOP" micro-label, reinforced by the coral `billing gRPC` token in the
 * summary. Teal steps aside entirely; strip the coral and a neutral mono CLI dump
 * remains. The lone cream strong token is the trace id the headline names; the
 * accentHover `$` is the reserved terminal prompt glyph and nothing more.
 *
 * React Server Component, settled final frame, no motion, no hooks. Every svg id
 * is prefixed "v4-observe-5-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  // accentHover is reserved for the terminal `$` prompt glyph; the subject is a
  // firing hop, so coral owns the single accent cluster and teal is otherwise unused.
  accentHover: "#99f6e4",
  coral: "#f0786a",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

const ID = "v4-observe-5-";

interface Span {
  /** Box-drawing connector prefix, e.g. "" / "├─ " / "└─ ". */
  readonly connector: string;
  readonly name: string;
  /** Transport / span-kind tag shown in the middle column. */
  readonly kind: string;
  readonly duration: string;
  /** The slow span is tinted coral and flagged. */
  readonly slow?: boolean;
  /** The GraphQL root span carries the whole-trace self-time. */
  readonly root?: boolean;
}

// The settled span tree for `nitro trace 4b1c8f2a` (EShops checkout). The root is
// the GraphQL operation; the billing gRPC hop is the degraded span. Each hop owns
// its self-time; the request total is restated in the summary line.
const SPANS: readonly Span[] = [
  {
    connector: "",
    name: "checkout",
    kind: "graphql",
    duration: "231ms",
    root: true,
  },
  {
    connector: "├─ ",
    name: "users-svc.GetProfile",
    kind: "rest",
    duration: "9ms",
  },
  {
    connector: "├─ ",
    name: "catalog.GetCart",
    kind: "rest",
    duration: "12ms",
  },
  {
    connector: "├─ ",
    name: "billing.Charge",
    kind: "grpc",
    duration: "214ms",
    slow: true,
  },
  {
    connector: "└─ ",
    name: "orders.Insert",
    kind: "pg",
    duration: "6ms",
  },
];

const ROW_Y = [56, 71, 86, 101, 116] as const;

export function ObserveVariant5({ className }: ObserveVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          trace replay
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 160"
            width="100%"
            role="img"
            aria-label="Nitro CLI replaying trace 4b1c8f2a: the resolved checkout span tree with per-hop timings, the billing gRPC hop flagged slow at 214 ms of a 231 ms total."
            style={{ display: "block", fontFamily: MONO }}
          >
            {/* ---- Terminal tile: the `nitro trace` artifact ---- */}
            <rect
              x={8}
              y={2}
              width={304}
              height={156}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Title bar: tool name left, cli kind tag right, closed by a divider. */}
            <text x={16} y={15} fill={C.inkDim} fontSize={9} fontWeight={600}>
              nitro
            </text>
            <text
              x={304}
              y={15}
              textAnchor="end"
              fill={C.navLabel}
              fontSize={8}
              letterSpacing="0.08em"
            >
              cli
            </text>
            <line
              x1={8}
              y1={22}
              x2={312}
              y2={22}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Command line: the trace id is the one cream strong token. */}
            <text x={16} y={38} fill={C.accentHover} fontSize={9}>
              $
            </text>
            <text x={27} y={38} fill={C.inkDim} fontSize={9}>
              nitro trace
            </text>
            <text
              x={90}
              y={38}
              fill={C.heading}
              fontSize={9}
              fontWeight={600}
              style={{ fontVariantNumeric: "tabular-nums" }}
            >
              4b1c8f2a
            </text>
            <text
              x={300}
              y={38}
              textAnchor="end"
              fill={C.navLabel}
              fontSize={8}
              letterSpacing="0.04em"
            >
              5 spans
            </text>

            {/* Span tree: connector + name, kind tag, right-aligned self-time. */}
            {SPANS.map((span, i) => {
              const y = ROW_Y[i];
              const durColor = span.slow
                ? C.coral
                : span.root
                  ? C.ink
                  : C.navLabel;
              return (
                <g key={`${ID}span-${i}`}>
                  <text x={16} y={y} fontSize={8.5}>
                    <tspan fill={C.navLabel}>{span.connector}</tspan>
                    <tspan fill={C.ink} fontWeight={span.root ? 600 : 400}>
                      {span.name}
                    </tspan>
                  </text>
                  <text x={140} y={y} fill={C.navLabel} fontSize={8}>
                    {span.kind}
                  </text>
                  <text
                    x={232}
                    y={y}
                    textAnchor="end"
                    fill={durColor}
                    fontSize={8.5}
                    fontWeight={span.slow ? 600 : 400}
                    style={{ fontVariantNumeric: "tabular-nums" }}
                  >
                    {span.duration}
                  </text>
                </g>
              );
            })}

            {/* Signature coral callout pinned to the slow billing hop (row 4). */}
            <line
              x1={206}
              y1={104}
              x2={232}
              y2={104}
              stroke={C.coral}
              strokeWidth={2}
            />
            <circle cx={238} cy={99} r={2.5} fill={C.coral} />
            <line
              x1={240}
              y1={98.5}
              x2={256}
              y2={92}
              stroke={C.coral}
              strokeWidth={1}
            />
            <text
              x={260}
              y={94}
              fill={C.coral}
              fontSize={7}
              letterSpacing="0.08em"
            >
              SLOW HOP
            </text>

            {/* Divider above the honest whole-trace summary. */}
            <line
              x1={16}
              y1={131}
              x2={304}
              y2={131}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Summary line: the real request total + the flagged hop. */}
            <text x={16} y={145} fontSize={8.5}>
              <tspan fill={C.navLabel}>total </tspan>
              <tspan
                fill={C.ink}
                fontWeight={600}
                style={{ fontVariantNumeric: "tabular-nums" }}
              >
                231ms
              </tspan>
              <tspan fill={C.inkDim}> &middot; 93% in </tspan>
              <tspan fill={C.coral}>billing gRPC</tspan>
            </text>
          </svg>
        </div>

        {/* Dashed-rule caption: real, replayable evidence per trace id. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            every hop and its self-time, replayed from one trace id
          </p>
        </div>
      </div>
    </div>
  );
}
