/**
 * Build-loop scene, variant 3 (Nitro terminal: dotnet build source-gen console).
 *
 * A small, self-contained terminal crop in the Nitro / Banana Cake Pop
 * GitHub-dark style: a card on `nitro.bg` with a 1px `nitro.border` hairline,
 * 6px corners, and a single thin chrome row carrying three traffic dots. The
 * body renders the settled FINAL frame of a `dotnet build` for an EShops Catalog
 * API: the `dotnet build` command, three streamed Hot Chocolate source-generator
 * emit lines (schema, resolvers, DataLoaders), a single line reporting the
 * per-generator emit cost, and a green "Build succeeded" carrying the whole-build
 * elapsed total.
 *
 * The generator emit cost (0.4s) and the whole-build total (1.8s) sit on separate
 * lines, so the generator cost is never misread as the entire build time.
 *
 * Static by design: no animation, no motion, no hooks. Nitro palette only,
 * applied via inline `style`. Self-contained; element ids prefixed "build-v3-".
 */

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface BuildVariant3Props {
  readonly className?: string;
}

type Tone = "prompt" | "command" | "ns" | "arrow" | "path" | "cost" | "metric";

interface Segment {
  readonly text: string;
  readonly tone: Tone;
}

type Line =
  | { readonly kind: "blank" }
  | { readonly kind: "row"; readonly segments: readonly Segment[] }
  | { readonly kind: "success"; readonly text: string };

// The captured stdout of `dotnet build` for an EShops Catalog API: the command,
// three source-generator emit lines (namespace -> emitted artifact), the single
// per-generator emit cost on its own line, then the success total.
const LINES: readonly Line[] = [
  {
    kind: "row",
    segments: [
      { text: "$ ", tone: "prompt" },
      { text: "dotnet build", tone: "command" },
    ],
  },
  { kind: "blank" },
  {
    kind: "row",
    segments: [
      { text: "Catalog.Api", tone: "ns" },
      { text: " -> ", tone: "arrow" },
      { text: "schema.graphql", tone: "path" },
    ],
  },
  {
    kind: "row",
    segments: [
      { text: "Catalog.Api", tone: "ns" },
      { text: " -> ", tone: "arrow" },
      { text: "Catalog.Resolvers.g.cs", tone: "path" },
    ],
  },
  {
    kind: "row",
    segments: [
      { text: "Catalog.Api", tone: "ns" },
      { text: " -> ", tone: "arrow" },
      { text: "ProductByIdDataLoader.g.cs", tone: "path" },
    ],
  },
  {
    kind: "row",
    segments: [
      { text: "HotChocolate.Types.Analyzers", tone: "cost" },
      { text: ": emitted in ", tone: "cost" },
      { text: "0.4s", tone: "metric" },
    ],
  },
  { kind: "blank" },
  { kind: "success", text: "Build succeeded in 1.8s" },
];

const TONE_COLOR: Record<Tone, string> = {
  prompt: nitro.accentHover,
  command: nitro.textStrong,
  ns: nitro.blue,
  arrow: nitro.textDim,
  path: nitro.textSecondary,
  cost: nitro.textDim,
  metric: nitro.text,
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

export function BuildVariant3({ className }: BuildVariant3Props) {
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
                <div key={`build-v3-line-${index}`} style={{ height: "9px" }} />
              );
            }

            if (line.kind === "success") {
              return (
                <div
                  key={`build-v3-line-${index}`}
                  style={{
                    ...rowStyle,
                    display: "flex",
                    alignItems: "center",
                    gap: "7px",
                    color: nitro.successText,
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
                    <path d="M2.4 6.3 5 8.7 9.6 3.4" />
                  </svg>
                  <span>{line.text}</span>
                </div>
              );
            }

            return (
              <div key={`build-v3-line-${index}`} style={rowStyle}>
                {line.segments.map((seg, segIndex) => (
                  <span
                    key={segIndex}
                    style={{
                      color: TONE_COLOR[seg.tone],
                      fontStyle: seg.tone === "cost" ? "italic" : "normal",
                    }}
                  >
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
