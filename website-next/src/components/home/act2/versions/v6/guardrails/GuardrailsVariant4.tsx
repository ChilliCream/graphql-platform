import type { CSSProperties, ReactNode } from "react";

/**
 * "Release safety" hook illustration, v6 bespoke: the break becomes a compiler
 * error.
 *
 * A calm editor pane for a consuming client. The surrounding lines stay quiet
 * grey, but the field access `order.total` carries a single coral squiggle, and
 * a settled error rail reads `Order.total does not exist`. The function declares
 * its parameter as `order: Order`, so the removed field surfaces as a red
 * compiler error in this file, long before any user hits it. Coral is rationed
 * to exactly the broken token, the active-line tint, and the error rail; every
 * other glyph is grey.
 *
 * Static React Server Component: no hooks, no client APIs, settled final frame.
 * Dark cc-* palette only. Every svg id is prefixed "v6-guardrails-4-".
 */
interface GuardrailsVariant4Props {
  readonly className?: string;
}

const ID = "v6-guardrails-4-";

const MONO =
  'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace';
const CORAL = "#f0786a";
const DIM = "rgba(245, 241, 234, 0.55)";
const TOKEN = "rgba(245, 241, 234, 0.82)";
const EYEBROW = "#62748e";

const GUTTER: CSSProperties = {
  width: "1.5rem",
  flex: "0 0 auto",
  paddingRight: "0.75rem",
  textAlign: "right",
  color: EYEBROW,
};

interface CodeLineProps {
  readonly n: number;
  readonly highlight?: boolean;
  readonly children: ReactNode;
}

/** One editor row: a dim gutter number and grey code, optionally active-tinted. */
function CodeLine({ n, highlight = false, children }: CodeLineProps) {
  return (
    <div
      style={{
        display: "flex",
        whiteSpace: "pre",
        marginLeft: "-0.85rem",
        marginRight: "-0.85rem",
        paddingLeft: "0.85rem",
        paddingRight: "0.85rem",
        borderRadius: "4px",
        backgroundColor: highlight
          ? "rgba(240, 120, 106, 0.07)"
          : "transparent",
      }}
    >
      <span style={GUTTER}>{n}</span>
      <span style={{ color: DIM }}>{children}</span>
    </div>
  );
}

/** The broken field access: grey token with a single coral squiggle beneath. */
function BrokenField() {
  return (
    <span style={{ position: "relative", color: TOKEN }}>
      total
      <svg
        id={`${ID}squiggle`}
        aria-hidden="true"
        viewBox="0 0 36 4"
        preserveAspectRatio="none"
        width="100%"
        height="3"
        style={{
          position: "absolute",
          left: 0,
          right: 0,
          bottom: "-3px",
          overflow: "visible",
        }}
      >
        <path
          d="M0 2 q 1.8 -1.8 3.6 0 t 3.6 0 t 3.6 0 t 3.6 0 t 3.6 0 t 3.6 0 t 3.6 0 t 3.6 0 t 3.6 0 t 3.6 0"
          fill="none"
          stroke={CORAL}
          strokeWidth="1"
          strokeLinecap="round"
          vectorEffect="non-scaling-stroke"
        />
      </svg>
    </span>
  );
}

export function GuardrailsVariant4({ className }: GuardrailsVariant4Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      aria-hidden="true"
    >
      <div className="border-cc-card-border bg-cc-surface overflow-hidden rounded-xl border">
        {/* chrome: filename and a single-error count badge */}
        <div className="border-cc-card-border flex items-center justify-between border-b px-3.5 py-2.5">
          <span
            className="text-[0.7rem]"
            style={{ fontFamily: MONO, color: DIM }}
          >
            OrderSummary.tsx
          </span>
          <span
            className="text-[0.6rem]"
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: "5px",
              fontFamily: MONO,
              letterSpacing: "0.04em",
              color: CORAL,
              border: "1px solid rgba(240, 120, 106, 0.35)",
              backgroundColor: "rgba(240, 120, 106, 0.08)",
              borderRadius: "9999px",
              padding: "2px 8px",
            }}
          >
            <span
              style={{
                width: "5px",
                height: "5px",
                borderRadius: "9999px",
                backgroundColor: CORAL,
              }}
            />
            1 error
          </span>
        </div>

        {/* the consuming client: grey code, one coral squiggle */}
        <div
          className="px-3.5 py-3.5"
          style={{ fontFamily: MONO, fontSize: "0.7rem", lineHeight: 1.95 }}
        >
          <CodeLine n={16}>function Receipt(order: Order) {"{"}</CodeLine>
          <CodeLine n={17}>{"  return formatMoney("}</CodeLine>
          <CodeLine n={18} highlight>
            {"    order."}
            <BrokenField />
            {","}
          </CodeLine>
          <CodeLine n={19}>{"  );"}</CodeLine>
          <CodeLine n={20}>{"}"}</CodeLine>
        </div>

        {/* the hero: a settled red compiler error */}
        <div
          className="border-cc-card-border flex items-center gap-2 border-t px-3.5 py-2.5"
          style={{ backgroundColor: "rgba(240, 120, 106, 0.05)" }}
        >
          <svg
            id={`${ID}error-mark`}
            aria-hidden="true"
            viewBox="0 0 12 12"
            width="13"
            height="13"
            fill="none"
            stroke={CORAL}
            strokeWidth="1.4"
            strokeLinecap="round"
            strokeLinejoin="round"
            style={{ flex: "0 0 auto" }}
          >
            <circle cx="6" cy="6" r="4.7" />
            <path d="M4.3 4.3 7.7 7.7M7.7 4.3 4.3 7.7" />
          </svg>
          <span
            className="text-[0.7rem]"
            style={{ fontFamily: MONO, color: CORAL }}
          >
            Order.total does not exist
          </span>
        </div>
      </div>
    </div>
  );
}
