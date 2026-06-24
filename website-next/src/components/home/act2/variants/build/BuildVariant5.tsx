/**
 * "Build loop" scene illustration (variant 5, Strawberry Shake client snippet).
 *
 * A small, self-contained code card styled as a cropped GitHub-dark editor: a
 * single thin title row (a `.cs` file tab with a generated-file glyph) above a
 * read-only snippet of the typed Strawberry Shake .NET client calling the EShops
 * Catalog API. The body is ~6 lines of C#: resolve the generated client, await
 * the generated `GetProduct.ExecuteAsync(id)` operation, then read strongly
 * typed fields off the result. The point of the scene is that the same C# types
 * flow from the Hot Chocolate server straight into the generated client, so the
 * call site is fully typed in one language.
 *
 * Rendered as the settled final frame: static, no animation, no motion, no
 * hooks. Chrome, syntax token colors, and spacing come from the shared `nitro`
 * palette via inline styles; every SVG / element id is prefixed `build-v5-`.
 */

import type { CSSProperties, ReactNode } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface BuildVariant5Props {
  readonly className?: string;
}

export function BuildVariant5({ className }: BuildVariant5Props) {
  return (
    <div
      className={["mx-auto w-full max-w-sm select-none", className ?? ""].join(
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
        {/* thin title row: generated-file glyph + file name + client badge */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            height: 32,
            padding: "0 12px",
            background: nitro.surface,
            borderBottom: `1px solid ${nitro.border}`,
          }}
        >
          <FileGlyph />
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: 11.5,
              color: nitro.textStrong,
            }}
          >
            Program.cs
          </span>
          <span
            style={{
              marginLeft: "auto",
              display: "inline-flex",
              alignItems: "center",
              height: 17,
              padding: "0 7px",
              borderRadius: 9999,
              border: `1px solid ${nitro.border}`,
              fontFamily: nitro.mono,
              fontSize: 9.5,
              letterSpacing: "0.04em",
              color: nitro.textSecondary,
            }}
          >
            StrawberryShake
          </span>
        </div>

        {/* read-only C# snippet of the generated typed client call */}
        <div style={{ background: nitro.bg, padding: "10px 0" }}>
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

/** Generated-source-file glyph in the title row. */
function FileGlyph() {
  return (
    <svg
      aria-hidden="true"
      width="13"
      height="13"
      viewBox="0 0 16 16"
      fill="none"
      style={{ flex: "0 0 auto", color: nitro.accentHover }}
    >
      <path
        d="M3.5 1.5h5L12.5 5.5v9a1 1 0 0 1-1 1h-8a1 1 0 0 1-1-1v-12a1 1 0 0 1 1-1Z"
        stroke="currentColor"
        strokeWidth="1.2"
        strokeLinejoin="round"
      />
      <path
        d="M8.5 1.5v4h4"
        stroke="currentColor"
        strokeWidth="1.2"
        strokeLinejoin="round"
      />
    </svg>
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
        minHeight: 20,
        fontFamily: nitro.mono,
        fontSize: 12,
        lineHeight: "20px",
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
const Member = span(nitro.synField);
const Plain = span(nitro.synPunct);
const Punct = span(nitro.synPunct);
const Comment = span(nitro.synComment);

/* The generated Strawberry Shake client call against the EShops Catalog API. */
const CODE_LINES: readonly ReactNode[] = [
  <>
    <Comment>{"// strongly typed, generated from the schema"}</Comment>
  </>,
  <>
    <Kw>var</Kw>
    <Plain> client </Plain>
    <Punct>= </Punct>
    <Member>provider</Member>
    <Punct>.</Punct>
    <Member>GetRequiredService</Member>
    <Punct>&lt;</Punct>
    <Type>ICatalogClient</Type>
    <Punct>&gt;();</Punct>
  </>,
  <> </>,
  <>
    <Kw>var</Kw>
    <Plain> result </Plain>
    <Punct>= </Punct>
    <Kw>await</Kw>
    <Plain> </Plain>
    <Member>client</Member>
    <Punct>.</Punct>
    <Member>GetProduct</Member>
    <Punct>.</Punct>
    <Member>ExecuteAsync</Member>
    <Punct>(</Punct>
    <Member>id</Member>
    <Punct>);</Punct>
  </>,
  <> </>,
  <>
    <Type>Product</Type>
    <Plain> product </Plain>
    <Punct>= </Punct>
    <Member>result</Member>
    <Punct>.</Punct>
    <Member>Data</Member>
    <Punct>!.</Punct>
    <Member>ProductById</Member>
    <Punct>;</Punct>
  </>,
  <>
    <Member>Console</Member>
    <Punct>.</Punct>
    <Member>WriteLine</Member>
    <Punct>(</Punct>
    <Member>product</Member>
    <Punct>.</Punct>
    <Member>Name</Member>
    <Punct>);</Punct>
    <Plain> </Plain>
    <Comment>{'// => "Aeon Runner"'}</Comment>
  </>,
];
