/**
 * Release-safety scene, variant 4 - "Build-drift console".
 *
 * A small, self-contained terminal crop in the Nitro / Banana Cake Pop
 * GitHub-dark style: a card on `nitro.bg` with a 1px `nitro.border` hairline,
 * 6px corners, and a single thin chrome row carrying three traffic dots. The
 * body renders the settled FINAL frame of a `dotnet build` for an EShops
 * storefront whose Strawberry Shake client was regenerated against a server
 * schema where `Product.rating` was retyped from `Int!` to `Float`. The
 * generated property is now `double?`, so the hand-written code that still
 * assigns it to an `int` no longer compiles: one CS0266 diagnostic with a
 * file/line/column source span, followed by a red "Build FAILED".
 *
 * Compact by design: roughly five output rows, one error, no surrounding app
 * chrome beyond the terminal traffic dots. Static: renders the settled frame
 * only, no animation, no motion package, no client hooks. The Nitro palette is
 * applied via inline `style={{}}` with `nitro.*` values (intentional here; the
 * rest of the site uses cc-* tokens). Every element id is prefixed
 * "guardrails-v4-".
 */
import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface GuardrailsVariant4Props {
  readonly className?: string;
}

type Tone =
  | "prompt"
  | "command"
  | "ns"
  | "arrow"
  | "path"
  | "loc"
  | "code"
  | "kind"
  | "muted"
  | "detail";

interface Segment {
  readonly text: string;
  readonly tone: Tone;
}

type Line =
  | { readonly kind: "blank" }
  | { readonly kind: "row"; readonly segments: readonly Segment[] }
  | { readonly kind: "failure"; readonly text: string };

// The captured stdout of `dotnet build` for the EShops web storefront after its
// Strawberry Shake client was regenerated: the command, the project being
// compiled, then the single CS0266 error with its source span (the retyped
// field is now double? and no longer fits the int it is assigned to), and the
// red build-failed summary.
const LINES: readonly Line[] = [
  {
    kind: "row",
    segments: [
      { text: "$ ", tone: "prompt" },
      { text: "dotnet build", tone: "command" },
    ],
  },
  {
    kind: "row",
    segments: [
      { text: "WebStorefront", tone: "ns" },
      { text: " -> ", tone: "arrow" },
      { text: "EShops.GraphQL.Client", tone: "path" },
    ],
  },
  { kind: "blank" },
  {
    kind: "row",
    segments: [
      { text: "ProductSummary.cs", tone: "path" },
      { text: "(42,28)", tone: "loc" },
      { text: ": ", tone: "muted" },
      { text: "error CS0266", tone: "kind" },
      { text: ":", tone: "muted" },
    ],
  },
  {
    kind: "row",
    segments: [
      { text: "  cannot convert ", tone: "detail" },
      { text: "double?", tone: "code" },
      { text: " to ", tone: "detail" },
      { text: "int", tone: "code" },
      { text: " (Product.rating: Int! -> Float)", tone: "detail" },
    ],
  },
  { kind: "blank" },
  { kind: "failure", text: "Build FAILED" },
];

const TONE_COLOR: Record<Tone, string> = {
  prompt: nitro.accentHover,
  command: nitro.textStrong,
  ns: nitro.blue,
  arrow: nitro.textDim,
  path: nitro.textSecondary,
  loc: nitro.textDim,
  code: nitro.synType,
  kind: nitro.errorText,
  muted: nitro.textDim,
  detail: nitro.text,
};

/** GitHub-dark traffic-light dots in the terminal chrome row. */
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

export function GuardrailsVariant4({ className }: GuardrailsVariant4Props) {
  const rowStyle = {
    fontFamily: nitro.mono,
    fontSize: "12px",
    lineHeight: "1.85",
    whiteSpace: "pre" as const,
  };

  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
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
        {/* Single thin chrome row: traffic dots only. */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            padding: "9px 12px",
            backgroundColor: nitro.surface,
            borderBottom: `1px solid ${nitro.border}`,
          }}
        >
          <ChromeDots />
        </div>

        {/* Terminal body: the settled dotnet build output. */}
        <div style={{ padding: "13px 14px 14px" }}>
          {LINES.map((line, index) => {
            if (line.kind === "blank") {
              return (
                <div
                  key={`guardrails-v4-line-${index}`}
                  style={{ height: "9px" }}
                />
              );
            }

            if (line.kind === "failure") {
              return (
                <div
                  key={`guardrails-v4-line-${index}`}
                  style={{
                    ...rowStyle,
                    display: "flex",
                    alignItems: "center",
                    gap: "7px",
                    color: nitro.errorText,
                  }}
                >
                  <svg
                    aria-hidden="true"
                    viewBox="0 0 12 12"
                    width="12"
                    height="12"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.7"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    style={{ flex: "0 0 auto" }}
                  >
                    <circle cx="6" cy="6" r="4.6" />
                    <path d="M4.2 4.2 7.8 7.8M7.8 4.2 4.2 7.8" />
                  </svg>
                  <span>{line.text}</span>
                </div>
              );
            }

            return (
              <div key={`guardrails-v4-line-${index}`} style={rowStyle}>
                {line.segments.map((seg, segIndex) => (
                  <span key={segIndex} style={{ color: TONE_COLOR[seg.tone] }}>
                    {seg.text}
                  </span>
                ))}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
