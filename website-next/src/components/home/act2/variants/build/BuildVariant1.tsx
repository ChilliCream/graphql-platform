import type { CSSProperties, ReactNode } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface BuildVariant1Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration (variant 1, annotated `[QueryType]` C# snippet).
 *
 * A small GitHub-dark code card showing the implementation-first C# source that
 * Hot Chocolate's source generator turns into a GraphQL schema: a `[QueryType]`
 * partial class `ProductApi` with a `GetProduct` resolver that returns an EShops
 * `Product`. A single thin title row carries the file name; below it the read-only
 * editor body renders the annotated partial class with a line-number gutter and
 * the exact GitHub-dark C# token colors.
 *
 * The metaphor: plain C# is the source the schema is generated from. Rendered as
 * the settled final frame, no animation. All chrome, colors, and spacing come
 * from the shared `nitro` palette via inline styles; every SVG id is prefixed
 * `build-v1-`.
 */
export function BuildVariant1({ className }: BuildVariant1Props) {
  return (
    <div
      className={["mx-auto w-full max-w-xs select-none", className ?? ""].join(
        " ",
      )}
      style={{ fontFamily: nitro.font, fontSize: 12, color: nitro.text }}
    >
      <div
        style={{
          background: nitro.bg,
          border: `1px solid ${nitro.border}`,
          borderRadius: nitro.radius,
          overflow: "hidden",
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
        }}
      >
        {/* thin title row: file-type glyph + the source file name */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 7,
            height: 30,
            padding: "0 11px",
            borderBottom: `1px solid ${nitro.border}`,
            background: nitro.surface,
          }}
        >
          <CSharpGlyph />
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: 11,
              color: nitro.textSecondary,
            }}
          >
            ProductApi.cs
          </span>
        </div>

        {/* read-only editor body */}
        <div style={{ background: nitro.bg, padding: "8px 0" }}>
          {CODE_LINES.map((line, i) => (
            <CodeLine key={i} n={i + 1}>
              {line}
            </CodeLine>
          ))}
        </div>
      </div>
    </div>
  );
}

/** The C# file-type glyph in the title row. */
function CSharpGlyph() {
  return (
    <span
      role="img"
      aria-label="C# source file"
      style={{
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        width: 16,
        height: 16,
        borderRadius: 3,
        background: "#512bd4",
        color: "#ffffff",
        fontFamily: nitro.mono,
        fontSize: 10,
        fontWeight: 700,
        letterSpacing: "-0.02em",
      }}
    >
      C#
    </span>
  );
}

/** One code line with a Monaco-style line-number gutter. */
function CodeLine({
  n,
  children,
}: {
  readonly n: number;
  readonly children: ReactNode;
}) {
  return (
    <div
      style={{
        display: "flex",
        minHeight: 19,
        fontFamily: nitro.mono,
        fontSize: 12,
        lineHeight: "19px",
        whiteSpace: "pre",
      }}
    >
      <span
        style={{
          width: 30,
          flex: "0 0 auto",
          textAlign: "right",
          paddingRight: 12,
          color: nitro.textDim,
          userSelect: "none",
        }}
      >
        {n}
      </span>
      <span style={{ flex: 1, minWidth: 0 }}>{children}</span>
    </div>
  );
}

/* ── GitHub-dark C# syntax spans ─────────────────────────────────────────────── */

const span = (
  color: string,
): ((p: { readonly children: ReactNode }) => ReactNode) =>
  function Span({ children }) {
    const style: CSSProperties = { color };
    return <span style={style}>{children}</span>;
  };

const Kw = span(nitro.synKeyword);
const Type = span(nitro.synType);
const Method = span(nitro.synField);
const Attr = span(nitro.synName);
const Punct = span(nitro.synPunct);

/* The annotated `[QueryType]` partial class the schema is generated from. */
const CODE_LINES: readonly ReactNode[] = [
  <>
    <Punct>[</Punct>
    <Attr>QueryType</Attr>
    <Punct>]</Punct>
  </>,
  <>
    <Kw>public partial class </Kw>
    <Type>ProductApi</Type>
  </>,
  <>
    <Punct>{"{"}</Punct>
  </>,
  <>
    {"  "}
    <Kw>public </Kw>
    <Type>Product</Type>
    <Punct> </Punct>
    <Method>GetProduct</Method>
    <Punct>(</Punct>
    <Type>int</Type>
    <Punct> id)</Punct>
  </>,
  <>
    {"    "}
    <Punct>{"=> "}</Punct>
    <Method>_catalog</Method>
    <Punct>.</Punct>
    <Method>Find</Method>
    <Punct>(id);</Punct>
  </>,
  <>
    <Punct>{"}"}</Punct>
  </>,
];
