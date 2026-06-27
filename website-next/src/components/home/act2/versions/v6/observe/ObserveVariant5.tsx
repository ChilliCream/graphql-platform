/**
 * "Production view" scene, v6 bespoke hook: a RESOLVED `nitro trace` terminal.
 *
 * The resolution beat. A single cropped terminal pane (the only green-dominant
 * tile of the set) prints the settled output of `nitro trace 4b1c8f2a` for the
 * EShops checkout request. A teal `[ops@nitro:~]$` prompt runs the command, then
 * an ASCII span tree replays below it with box-drawing connectors and per-hop
 * mono timings. Every hop now carries a small green check, including the
 * `billing.Charge` gRPC hop that used to be the slow span: its row glows faintly
 * green and the closing line shows the win, 214ms down to 18ms, resolved end to
 * end. Read the trace, see it green.
 *
 * Static React Server Component: no hooks, no client APIs, settled final frame.
 * Dark cc-* palette only; the green (healthy) status color is earned, it encodes
 * a real resolved-and-healthy trace. Every svg id is prefixed "v6-observe-5-".
 */

import type { CSSProperties } from "react";

interface ObserveVariant5Props {
  readonly className?: string;
}

const C = {
  bg: "#0b0f1a",
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  border: "rgba(245, 241, 234, 0.12)",
  navLabel: "#62748e",
  accent: "#5eead4",
  accentHover: "#99f6e4",
  healthy: "#34d399",
  blue: "#58a6ff",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

const ID = "v6-observe-5-";

interface Span {
  /** Box-drawing connector prefix, e.g. "|- " / "`- " drawn with real glyphs. */
  readonly connector: string;
  readonly name: string;
  /** Transport / span-kind tag shown after the name. */
  readonly kind: string;
  readonly duration: string;
  readonly root?: boolean;
  /** the previously-slow billing hop, now resolved end to end. */
  readonly resolved?: boolean;
}

// The settled, now-healthy span tree for `nitro trace 4b1c8f2a` (EShops
// checkout). Same hops as the live trace, but the billing gRPC span that ran
// 214ms has been resolved to 18ms, so the whole tree replays green.
const SPANS: readonly Span[] = [
  {
    connector: "",
    name: "checkout",
    kind: "graphql",
    duration: "58ms",
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
    duration: "18ms",
    resolved: true,
  },
  {
    connector: "└─ ",
    name: "orders.Insert",
    kind: "pg",
    duration: "6ms",
  },
];

const ROW: CSSProperties = {
  fontFamily: C.mono,
  fontSize: 12,
  lineHeight: "20px",
  whiteSpace: "pre",
};

/** Small green check stamped on every settled hop. */
function Check() {
  return (
    <svg
      width="12"
      height="12"
      viewBox="0 0 12 12"
      fill="none"
      aria-hidden="true"
      style={{ display: "block" }}
    >
      <path
        d="M2.4 6.3 L4.8 8.7 L9.6 3.2"
        stroke={C.healthy}
        strokeWidth="1.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** GitHub-dark traffic-light dots in the terminal chrome header. */
function ChromeDots() {
  const dots = ["#ff5f56", "#ffbd2e", "#27c93f"];
  return (
    <span
      aria-hidden="true"
      style={{ display: "inline-flex", alignItems: "center", gap: 7 }}
    >
      {dots.map((c) => (
        <span
          key={c}
          style={{
            width: 10,
            height: 10,
            borderRadius: 9999,
            backgroundColor: c,
            opacity: 0.85,
          }}
        />
      ))}
    </span>
  );
}

export function ObserveVariant5({ className }: ObserveVariant5Props) {
  return (
    <div
      className={["mx-auto w-full select-none", className ?? ""].join(" ")}
      style={{ maxWidth: 336 }}
    >
      <div
        style={{
          border: `1px solid ${C.border}`,
          borderRadius: 12,
          overflow: "hidden",
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
        }}
      >
        {/* code-chrome header: traffic dots + shell label + healthy pill */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 10,
            padding: "9px 12px",
            backgroundColor: C.surface,
            borderBottom: `1px solid ${C.border}`,
          }}
        >
          <span
            style={{ display: "inline-flex", alignItems: "center", gap: 10 }}
          >
            <ChromeDots />
            <span
              style={{
                fontFamily: C.mono,
                fontSize: 10,
                letterSpacing: "0.12em",
                color: C.navLabel,
              }}
            >
              nitro
            </span>
          </span>
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 6,
              padding: "2px 8px",
              borderRadius: 9999,
              border: `1px solid rgba(52, 211, 153, 0.4)`,
              background: "rgba(52, 211, 153, 0.08)",
              fontFamily: C.mono,
              fontSize: 9,
              letterSpacing: "0.1em",
              textTransform: "uppercase",
              color: C.healthy,
              whiteSpace: "nowrap",
            }}
          >
            <span
              style={{
                width: 6,
                height: 6,
                borderRadius: 9999,
                backgroundColor: C.healthy,
              }}
            />
            all green
          </span>
        </div>

        {/* terminal body: the settled, resolved `nitro trace` output */}
        <div
          style={{
            padding: "13px 0 14px",
            background: `radial-gradient(130% 78% at 50% 0%, rgba(52, 211, 153, 0.06), transparent 62%), ${C.bg}`,
          }}
        >
          {/* command line with the teal ops prompt */}
          <div style={{ ...ROW, padding: "0 14px" }}>
            <span style={{ color: C.accent }}>[ops@nitro:~]</span>
            <span style={{ color: C.accentHover }}>$ </span>
            <span style={{ color: C.heading }}>nitro trace </span>
            <span style={{ color: C.blue }}>4b1c8f2a</span>
          </div>

          <div style={{ height: 9 }} />

          {/* span tree: connector + name + kind + green check + self-time */}
          {SPANS.map((span, index) => {
            const durColor = span.root ? C.heading : C.healthy;

            return (
              <div
                key={`${ID}span-${index}`}
                style={{
                  ...ROW,
                  display: "grid",
                  gridTemplateColumns: "minmax(0, 1fr) 44px 14px 44px",
                  alignItems: "center",
                  columnGap: 8,
                  padding: "1px 14px",
                  background: span.resolved
                    ? "rgba(52, 211, 153, 0.07)"
                    : "transparent",
                  boxShadow: span.resolved
                    ? `inset 2px 0 0 ${C.healthy}`
                    : undefined,
                }}
              >
                <span style={{ minWidth: 0, overflow: "hidden" }}>
                  <span style={{ color: C.navLabel }}>{span.connector}</span>
                  <span
                    style={{
                      color: span.root ? C.heading : C.ink,
                      fontWeight: span.root ? 600 : 400,
                    }}
                  >
                    {span.name}
                  </span>
                </span>
                <span style={{ color: C.navLabel }}>{span.kind}</span>
                <span style={{ display: "flex", justifyContent: "center" }}>
                  <Check />
                </span>
                <span
                  style={{
                    textAlign: "right",
                    color: durColor,
                    fontWeight: span.root || span.resolved ? 600 : 400,
                  }}
                >
                  {span.duration}
                </span>
              </div>
            );
          })}

          <div style={{ height: 9 }} />

          {/* summary: resolved end to end + the slow-hop win, 214ms -> 18ms */}
          <div
            style={{
              ...ROW,
              display: "flex",
              alignItems: "center",
              gap: 7,
              padding: "0 14px",
            }}
          >
            <Check />
            <span style={{ color: C.healthy, fontWeight: 600 }}>
              resolved end to end
            </span>
            <span style={{ color: C.inkDim }}>{"·  58ms"}</span>
          </div>
          <div style={{ ...ROW, fontSize: 11, padding: "2px 14px 0 33px" }}>
            <span style={{ color: C.navLabel }}>billing gRPC </span>
            <span
              style={{ color: C.inkDim, textDecorationLine: "line-through" }}
            >
              214ms
            </span>
            <span style={{ color: C.navLabel }}> → </span>
            <span style={{ color: C.healthy, fontWeight: 600 }}>18ms</span>
          </div>

          {/* trailing prompt + resting cursor block */}
          <div
            style={{
              ...ROW,
              display: "flex",
              alignItems: "center",
              padding: "8px 14px 0",
            }}
          >
            <span style={{ color: C.accent }}>[ops@nitro:~]</span>
            <span style={{ color: C.accentHover }}>$ </span>
            <span
              aria-hidden="true"
              style={{
                marginLeft: 2,
                display: "inline-block",
                width: 7,
                height: 14,
                borderRadius: 1,
                backgroundColor: C.accentHover,
                opacity: 0.85,
              }}
            />
          </div>
        </div>
      </div>
    </div>
  );
}
