import type { CSSProperties, ReactNode } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface BuildVariant2Props {
  readonly className?: string;
}

/**
 * "Build loop" scene illustration (variant 2, "Generated SDL snippet").
 *
 * This scene tells the Hot Chocolate codegen / dev story: you write an
 * implementation-first C# type and the source generator emits the GraphQL
 * schema. The widget is a single small code card with a thin GitHub-dark title
 * row (a `schema.graphql` filename plus a "generated" pill) over a read-only
 * editor body that shows the ~5 lines of SDL emitted for the EShops `Product`
 * type, syntax-highlighted with the exact Nitro / GitHub-dark GraphQL token
 * colors. A tiny caption underneath credits the emitting source class.
 *
 * One thing only: the generated SDL. No Nitro dashboards, tabs, or app chrome.
 * Rendered as the settled final frame, no animation. All colors and spacing come
 * from the shared `nitro` palette via inline styles; every SVG id is prefixed
 * `build-v2-`.
 */
export function BuildVariant2({ className }: BuildVariant2Props) {
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
        {/* thin title row: file glyph + name on the left, "generated" pill right */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 7,
            height: 30,
            padding: "0 10px",
            borderBottom: `1px solid ${nitro.border}`,
            background: nitro.surface,
          }}
        >
          <GraphqlGlyph />
          <span
            style={{
              fontFamily: nitro.mono,
              fontSize: 11.5,
              color: nitro.textSecondary,
            }}
          >
            schema.graphql
          </span>
          <span
            style={{
              marginLeft: "auto",
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              height: 18,
              padding: "0 7px",
              borderRadius: 9,
              fontSize: 9.5,
              letterSpacing: "0.03em",
              color: nitro.successText,
              background: "rgba(63, 208, 127, 0.12)",
              border: `1px solid rgba(63, 208, 127, 0.3)`,
            }}
          >
            <span
              aria-hidden="true"
              style={{
                width: 5,
                height: 5,
                borderRadius: "50%",
                background: nitro.successText,
              }}
            />
            generated
          </span>
        </div>

        {/* read-only SDL editor body with a line-number gutter */}
        <div style={{ background: nitro.bg, padding: "8px 0" }}>
          {SDL_LINES.map((line, i) => (
            <SdlLine key={i} n={i + 1}>
              {line}
            </SdlLine>
          ))}
        </div>
      </div>

      {/* caption: which source class emitted this SDL */}
      <p
        style={{
          margin: "8px 2px 0",
          fontSize: 10.5,
          lineHeight: 1.4,
          color: nitro.textDim,
        }}
      >
        Generated from{" "}
        <code
          style={{
            fontFamily: nitro.mono,
            fontSize: 10.5,
            color: nitro.textSecondary,
          }}
        >
          Product.cs
        </code>{" "}
        by the schema source generator.
      </p>
    </div>
  );
}

/** The pink GraphQL hexagon mark Nitro uses for schema documents. */
function GraphqlGlyph() {
  return (
    <svg
      width="13"
      height="13"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      style={{ flex: "0 0 auto" }}
    >
      <path
        d="M8 1.2 14 4.6v6.8L8 14.8 2 11.4V4.6L8 1.2Z"
        stroke={nitro.icGraphql}
        strokeWidth="1"
        strokeLinejoin="round"
      />
      <path
        d="M8 1.6 2.4 11.2M8 1.6l5.6 9.6M3 4.4l10 .2M3 11.6 13 4.6M3 4.6 13 11.6"
        stroke={nitro.icGraphql}
        strokeWidth="0.8"
        strokeLinecap="round"
        opacity="0.85"
      />
      <circle cx="8" cy="1.6" r="1.3" fill={nitro.icGraphql} />
      <circle cx="13.4" cy="4.6" r="1.3" fill={nitro.icGraphql} />
      <circle cx="13.4" cy="11.4" r="1.3" fill={nitro.icGraphql} />
      <circle cx="8" cy="14.4" r="1.3" fill={nitro.icGraphql} />
      <circle cx="2.6" cy="11.4" r="1.3" fill={nitro.icGraphql} />
      <circle cx="2.6" cy="4.6" r="1.3" fill={nitro.icGraphql} />
    </svg>
  );
}

/** One SDL line with a Monaco-style line-number gutter. */
function SdlLine({
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

/* ── GitHub-dark GraphQL SDL syntax spans ───────────────────────────────────── */

const span = (
  color: string,
): ((p: { readonly children: ReactNode }) => ReactNode) =>
  function Span({ children }) {
    const style: CSSProperties = { color };
    return <span style={style}>{children}</span>;
  };

const Kw = span(nitro.synKeyword);
const Type = span(nitro.synType);
const Field = span(nitro.synField);
const Punct = span(nitro.synPunct);

/* The 5-line SDL the source generator emits for the EShops `Product` type. */
const SDL_LINES: readonly ReactNode[] = [
  <>
    <Kw>type </Kw>
    <Type>Product</Type>
    <Punct> {"{"}</Punct>
  </>,
  <>
    {"  "}
    <Field>id</Field>
    <Punct>: </Punct>
    <Type>ID</Type>
    <Punct>!</Punct>
  </>,
  <>
    {"  "}
    <Field>name</Field>
    <Punct>: </Punct>
    <Type>String</Type>
    <Punct>!</Punct>
  </>,
  <>
    {"  "}
    <Field>price</Field>
    <Punct>: </Punct>
    <Type>Float</Type>
    <Punct>!</Punct>
  </>,
  <>
    <Punct>{"}"}</Punct>
  </>,
];
