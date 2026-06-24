import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface WorkflowsVariant3Props {
  readonly className?: string;
}

/**
 * Workflow scene, variant 3 ("Message mini-trace") as a compact cropped Nitro
 * telemetry widget.
 *
 * One small trace waterfall, four spans: the CreateReview command at the root,
 * its handler, the ReviewCreated publish (producer), and the consumer that picks
 * the event up. Each hop is a single labeled bar positioned by start/duration on
 * a shared time axis, tinted by span kind. A thin title row, then the four spans.
 *
 * Static (no animation library), settled final frame only. Uses the Nitro
 * palette via inline styles (deliberate for these illustrations). All svg ids
 * are prefixed "workflows-v3-".
 */

interface Span {
  /** Span label. */
  readonly name: string;
  /** Bar color (span kind tint). */
  readonly color: string;
  /** Span start, fraction of the trace [0,1]. */
  readonly start: number;
  /** Span duration, fraction of the trace [0,1]. */
  readonly width: number;
}

const TOTAL_MS = 96;

// CreateReview command at the root, then the handler, the ReviewCreated publish
// (producer), and the consumer span that picks the event up.
const SPANS: readonly Span[] = [
  {
    name: "CreateReview command",
    color: nitro.chThroughput,
    start: 0,
    width: 1,
  },
  {
    name: "CreateReviewHandler",
    color: nitro.chLatency,
    start: 0.06,
    width: 0.4,
  },
  {
    name: "publish ReviewCreated",
    color: nitro.warning,
    start: 0.32,
    width: 0.14,
  },
  {
    name: "ProductRatingProjection",
    color: nitro.active,
    start: 0.51,
    width: 0.43,
  },
];

export function WorkflowsVariant3({ className }: WorkflowsVariant3Props) {
  return (
    <div
      className={className}
      style={{
        margin: "0 auto",
        width: "100%",
        maxWidth: "20rem",
        userSelect: "none",
        fontFamily: nitro.font,
      }}
    >
      <div
        style={{
          borderRadius: nitro.radius,
          border: `1px solid ${nitro.border}`,
          background: nitro.card,
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
          overflow: "hidden",
        }}
      >
        {/* thin title row: trace name + total duration */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "8px 14px",
            borderBottom: `1px solid ${nitro.border}`,
            background: nitro.surface,
          }}
        >
          <span
            style={{ fontSize: 12, fontWeight: 600, color: nitro.textStrong }}
          >
            CreateReview
          </span>
          <span
            style={{
              marginLeft: "auto",
              fontFamily: nitro.mono,
              fontSize: 11,
              fontWeight: 600,
              color: nitro.text,
            }}
          >
            {TOTAL_MS} ms
          </span>
        </div>

        {/* waterfall: one labeled bar per span */}
        <div
          style={{
            padding: "12px 14px",
            display: "flex",
            flexDirection: "column",
            gap: 10,
          }}
        >
          {SPANS.map((span) => (
            <div key={span.name}>
              <div
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 6,
                  marginBottom: 4,
                }}
              >
                <span
                  aria-hidden="true"
                  style={{
                    width: 7,
                    height: 7,
                    borderRadius: 2,
                    background: span.color,
                    flex: "0 0 auto",
                  }}
                />
                <span style={{ fontSize: 11.5, color: nitro.text }}>
                  {span.name}
                </span>
                <span
                  style={{
                    marginLeft: "auto",
                    fontSize: 10.5,
                    fontFamily: nitro.mono,
                    color: nitro.textSecondary,
                  }}
                >
                  {Math.round(span.width * TOTAL_MS)} ms
                </span>
              </div>
              <div style={{ position: "relative", height: 8 }}>
                <span
                  style={{
                    position: "absolute",
                    left: `${span.start * 100}%`,
                    width: `${span.width * 100}%`,
                    height: 8,
                    borderRadius: 2,
                    background: span.color,
                  }}
                />
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
