interface ObserveVariant2Props {
  readonly className?: string;
}

/**
 * "Production view" scene, v4 "Generated Artifacts", variant 2: the distributed
 * trace waterfall Nitro emits for one request.
 *
 * Locked v4 PATTERN F (thin bars on a shared axis) inside a single cc-surface
 * artifact tile. The tile is the trace artifact for the `checkout` request: a
 * title bar (operation name + trace id left, the `94 ms` total right-aligned)
 * closed by a 1px divider, then four nested spans drawn as a waterfall on the
 * shared 0 -> 94 ms axis. Each span is an indented mono name, a right-aligned
 * span-kind tag (GraphQL / REST / gRPC / SELECT), and a rounded bar positioned by
 * its (start, end). All bars are monochrome cc-ink except the slow `billing` gRPC
 * hop, the one genuinely-firing span on the critical path.
 *
 * Status is the subject here, so coral (not teal) owns the single accent cluster:
 * the firing `billing` bar, plus the signature callout pinned to it (a 2.5px
 * coral dot at the bar end, a 1px coral leader into the right margin, the
 * load-bearing `63ms` duration token, a 2px underline tick, and a "SLOW HOP"
 * micro-label). Teal steps aside entirely; strip the coral and the waterfall
 * reads as neutral monochrome timing. Exactly one accent cluster, no competing
 * highlight.
 *
 * Literal content (operation `checkout`, trace id `4b1c8f2a`, spans and their
 * 0 / 8-18 / 18-81 / 81-94 ms timing, 63 ms bottleneck, 94 ms total) is borrowed
 * verbatim from the v1 sibling. React Server Component, settled final frame, no
 * motion. Every svg id is prefixed "v4-observe-2-".
 */

const C = {
  surface: "#0c1322",
  cardBorder: "rgba(245, 241, 234, 0.12)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  navLabel: "#62748e",
  heading: "#f5f0ea",
  // Frozen palette keeps teal for set-wide coherence; this cell's subject is a
  // firing hop, so coral owns the single accent cluster and teal is unused.
  accent: "#5eead4",
  coral: "#f0786a",
} as const;

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';

// Shared 0 -> 94 ms time axis mapped onto the track region inside the tile.
const TOTAL_MS = 94;
const X0 = 116;
const TRACK_W = 134;
const xAt = (ms: number) => X0 + (ms / TOTAL_MS) * TRACK_W;

// One waterfall row every 22 userspace px; the body opens below the divider.
const rowTop = (i: number) => 40 + i * 22;

interface TraceSpan {
  readonly label: string;
  readonly kind: string;
  readonly start: number;
  readonly end: number;
  readonly depth: number;
  readonly firing: boolean;
}

const SPANS: readonly TraceSpan[] = [
  {
    label: "checkout",
    kind: "GraphQL",
    start: 0,
    end: 94,
    depth: 0,
    firing: false,
  },
  {
    label: "users-svc",
    kind: "REST",
    start: 8,
    end: 18,
    depth: 1,
    firing: false,
  },
  {
    label: "billing",
    kind: "gRPC",
    start: 18,
    end: 81,
    depth: 1,
    firing: true,
  },
  { label: "db", kind: "SELECT", start: 81, end: 94, depth: 1, firing: false },
] as const;

export function ObserveVariant2({ className }: ObserveVariant2Props) {
  // The coral callout attaches at the firing billing bar (row index 2).
  const billingTop = rowTop(2);
  const billingMid = billingTop + 4;
  const billingEnd = xAt(81);

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-5 backdrop-blur-sm">
        <p className="text-cc-nav-label font-mono text-[0.58rem] tracking-[0.15em] uppercase">
          distributed trace
        </p>

        <div className="mt-3">
          <svg
            viewBox="0 0 320 140"
            width="100%"
            role="img"
            aria-label="Distributed trace waterfall for the checkout request: 4 nested spans over 94 ms, with the billing gRPC hop the 63 ms bottleneck on the critical path, flagged coral."
            style={{ display: "block", fontFamily: MONO }}
          >
            {/* Artifact tile: the trace for one request. */}
            <rect
              x={8}
              y={2}
              width={304}
              height={134}
              rx={8}
              fill={C.surface}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Title bar: operation + trace id, total right-aligned. */}
            <text
              x={20}
              y={17}
              fill={C.heading}
              fontSize={11.5}
              fontWeight={600}
            >
              checkout
            </text>
            <text x={84} y={17} fill={C.navLabel} fontSize={8.5}>
              4b1c8f2a
            </text>
            <text
              x={300}
              y={17}
              fill={C.ink}
              fontSize={9.5}
              textAnchor="end"
              style={{ fontVariantNumeric: "tabular-nums" }}
            >
              94 ms
            </text>
            <line
              x1={8}
              y1={25}
              x2={312}
              y2={25}
              stroke={C.cardBorder}
              strokeWidth={1}
            />

            {/* Faint nesting spine tying the children under the root span. */}
            <line
              x1={24}
              y1={rowTop(0) + 4}
              x2={24}
              y2={rowTop(3) + 4}
              stroke={C.inkFaint}
              strokeWidth={1}
            />

            {/* Dashed end-gridline marking the 94 ms edge of the axis. */}
            <line
              x1={xAt(94)}
              y1={rowTop(0) - 2}
              x2={xAt(94)}
              y2={rowTop(3) + 10}
              stroke={C.inkFaint}
              strokeWidth={1}
              strokeDasharray="3 3"
            />

            {/* Span rows: indented name, span-kind tag, bar on the time axis. */}
            {SPANS.map((span, i) => {
              const top = rowTop(i);
              const nameX = span.depth === 0 ? 20 : 30;
              return (
                <g key={`v4-observe-2-row-${span.label}`}>
                  <text
                    x={nameX}
                    y={top + 7}
                    fill={span.firing ? C.coral : C.ink}
                    fontSize={9.5}
                    fontWeight={span.firing ? 600 : 400}
                  >
                    {span.label}
                  </text>
                  <text
                    x={108}
                    y={top + 7}
                    fill={C.navLabel}
                    fontSize={7.5}
                    textAnchor="end"
                  >
                    {span.kind}
                  </text>
                  <rect
                    x={xAt(span.start)}
                    y={top}
                    width={xAt(span.end) - xAt(span.start)}
                    height={8}
                    rx={4}
                    fill={span.firing ? C.coral : C.ink}
                    fillOpacity={span.firing ? 0.9 : 0.45}
                  />
                </g>
              );
            })}

            {/* Signature callout (coral owns it): anchor dot -> leader -> 63ms. */}
            <circle
              cx={billingEnd + 3}
              cy={billingMid}
              r={2.5}
              fill={C.coral}
            />
            <path
              d={`M${(billingEnd + 3).toFixed(1)} ${billingMid} L254 80`}
              fill="none"
              stroke={C.coral}
              strokeWidth={1}
            />
            <text
              x={256}
              y={82}
              fill={C.coral}
              fontSize={11.5}
              fontWeight={600}
              style={{ fontVariantNumeric: "tabular-nums" }}
            >
              63ms
            </text>
            <line
              x1={256}
              y1={86}
              x2={284}
              y2={86}
              stroke={C.coral}
              strokeWidth={2}
            />
            <text
              x={256}
              y={97}
              fill={C.coral}
              fontSize={7}
              letterSpacing="0.08em"
            >
              SLOW HOP
            </text>
          </svg>
        </div>

        {/* Dashed caption footer: where the request actually spends its time. */}
        <div className="border-cc-ink-faint mt-4 border-t border-dashed pt-3">
          <p className="text-cc-ink-dim text-center text-xs">
            one gRPC hop, 63 of 94 ms
          </p>
        </div>
      </div>
    </div>
  );
}
