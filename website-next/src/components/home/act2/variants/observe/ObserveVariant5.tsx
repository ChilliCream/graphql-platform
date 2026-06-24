/**
 * "Production view" scene, variant 5 - an authentic STATIC Nitro terminal panel
 * (GitHub-dark): a `nitro trace <id>` console printing a compact span tree.
 *
 * A small cropped terminal: a code-chrome header (three traffic-light dots + a
 * shell label), then the settled FINAL output of `nitro trace 4b1c8f2a` for an
 * EShops checkout request. The body prints a compact 5-line span tree drawn with
 * box-drawing connectors, each hop carrying its own duration. The billing gRPC
 * hop is flagged inline as the slow span (amber duration + a "SLOW" tag), while
 * the GraphQL root, the users-svc and catalog REST hops, and the Postgres write
 * stay calm. A closing total-duration summary line carries the whole-trace
 * elapsed so the single 214ms billing hop is never read as the request total.
 *
 * Styling clones the Nitro / Banana Cake Pop terminal by eye: `nitro.bg` card
 * with a 1px `nitro.border` hairline, 6px corners, `nitro.mono` for all output,
 * and per-span tints from the shared `nitro` palette applied inline via `style`.
 *
 * Static by design: no animation, no motion, no hooks. Self-contained; element
 * ids prefixed "observe-v5-".
 */

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface ObserveVariant5Props {
  readonly className?: string;
}

interface Span {
  /** Box-drawing connector prefix, e.g. "├─ " / "└─ ". */
  readonly connector: string;
  readonly name: string;
  /** Transport / span-kind tag shown after the name. */
  readonly kind: string;
  readonly duration: string;
  /** The slow span is tinted amber and flagged. */
  readonly slow?: boolean;
}

// The settled span tree for `nitro trace 4b1c8f2a` (EShops checkout). The root
// is the GraphQL operation; the billing gRPC hop is the degraded span. Each hop
// owns its self-time; the request total is reported separately in the summary.
const SPANS: readonly Span[] = [
  {
    connector: "",
    name: "checkout",
    kind: "graphql",
    duration: "231ms",
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

/** GitHub-dark traffic-light dots in the terminal chrome header. */
function ChromeDots() {
  const dots = ["#ff5f56", "#ffbd2e", "#27c93f"];
  return (
    <span
      aria-hidden="true"
      style={{ display: "inline-flex", alignItems: "center", gap: "7px" }}
    >
      {dots.map((c) => (
        <span
          key={c}
          style={{
            width: "10px",
            height: "10px",
            borderRadius: "9999px",
            backgroundColor: c,
            opacity: 0.85,
          }}
        />
      ))}
    </span>
  );
}

export function ObserveVariant5({ className }: ObserveVariant5Props) {
  const rowStyle = {
    fontFamily: nitro.mono,
    fontSize: "12px",
    lineHeight: "1.7",
    whiteSpace: "pre" as const,
  };

  return (
    <div
      className={["mx-auto w-full max-w-sm select-none", className ?? ""].join(
        " ",
      )}
      style={{ fontFamily: nitro.font }}
    >
      <div
        style={{
          backgroundColor: nitro.bg,
          border: `1px solid ${nitro.border}`,
          borderRadius: nitro.radius,
          overflow: "hidden",
        }}
      >
        {/* Code-chrome header: traffic dots + shell label. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: "10px",
            padding: "8px 12px",
            backgroundColor: nitro.surface,
            borderBottom: `1px solid ${nitro.border}`,
          }}
        >
          <ChromeDots />
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: "10px",
              letterSpacing: "0.12em",
              color: nitro.textSecondary,
            }}
          >
            nitro
          </span>
        </div>

        {/* Terminal body: the settled `nitro trace` output. */}
        <div style={{ padding: "14px 14px 16px" }}>
          {/* Command line. */}
          <div style={rowStyle}>
            <span style={{ color: nitro.accentHover }}>$ </span>
            <span style={{ color: nitro.textStrong }}>nitro trace </span>
            <span style={{ color: nitro.blue }}>4b1c8f2a</span>
          </div>

          <div style={{ height: "10px" }} />

          {/* Span tree. Each row is a box-drawing connector + span name +
              kind tag + a right-aligned, self-time duration. */}
          {SPANS.map((span, index) => {
            const isRoot = index === 0;
            const durColor = span.slow
              ? nitro.warning
              : isRoot
                ? nitro.text
                : nitro.textSecondary;

            return (
              <div
                key={`observe-v5-span-${index}`}
                style={{
                  ...rowStyle,
                  display: "flex",
                  alignItems: "baseline",
                  gap: "8px",
                }}
              >
                <span style={{ color: nitro.textDim, flex: "0 0 auto" }}>
                  {span.connector}
                </span>
                <span
                  style={{
                    color: isRoot ? nitro.textStrong : nitro.text,
                    fontWeight: isRoot ? 600 : 400,
                    flex: "0 0 auto",
                  }}
                >
                  {span.name}
                </span>
                <span style={{ color: nitro.textDim, flex: "0 0 auto" }}>
                  {span.kind}
                </span>
                {span.slow ? (
                  <span
                    style={{
                      color: nitro.warning,
                      border: `1px solid ${nitro.warning}`,
                      borderRadius: "4px",
                      padding: "0 5px",
                      fontSize: "9px",
                      letterSpacing: "0.08em",
                      lineHeight: 1.5,
                      flex: "0 0 auto",
                    }}
                  >
                    SLOW
                  </span>
                ) : null}
                <span
                  style={{
                    marginLeft: "auto",
                    color: durColor,
                    fontWeight: span.slow ? 600 : 400,
                    flex: "0 0 auto",
                  }}
                >
                  {span.duration}
                </span>
              </div>
            );
          })}

          <div style={{ height: "10px" }} />

          {/* Summary line: whole-trace total + the flagged hop callout. */}
          <div style={rowStyle}>
            <span style={{ color: nitro.textSecondary }}>total </span>
            <span style={{ color: nitro.text, fontWeight: 600 }}>231ms</span>
            <span style={{ color: nitro.textDim }}>{"  ·  93% in "}</span>
            <span style={{ color: nitro.warning, fontWeight: 600 }}>
              billing gRPC
            </span>
          </div>

          {/* Trailing prompt + resting cursor block. */}
          <div
            style={{
              ...rowStyle,
              display: "flex",
              alignItems: "center",
              marginTop: "6px",
            }}
          >
            <span style={{ color: nitro.accentHover }}>$ </span>
            <span
              aria-hidden="true"
              style={{
                marginLeft: "2px",
                display: "inline-block",
                width: "7px",
                height: "14px",
                borderRadius: "1px",
                backgroundColor: nitro.accentHover,
                opacity: 0.85,
              }}
            />
          </div>
        </div>
      </div>
    </div>
  );
}
