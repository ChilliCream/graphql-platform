/**
 * Agentic coding, variant 4: a SKILL.md snippet (static Nitro code card).
 *
 * A small GitHub-dark code card showing ~6 lines of a `SKILL.md` agent skill: a
 * YAML frontmatter block (name / description) fenced by `---`, then one fenced
 * GraphQL operation the agent runs against `/graphql/mcp`. It reads as a slice of
 * the real editor: a single thin title row (filename + language badge), then a
 * line-numbered, hand-tokenized code body using the exact Nitro / GitHub-dark
 * editor syntax colors.
 *
 * Static by design: the settled final frame, no motion package, no hooks, no
 * "use client". SVG ids are prefixed "feedback-v4-". Colors come from the shared
 * Nitro palette via inline style, not the site cc-* tokens. Believable EShops
 * sample data.
 */
import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface FeedbackVariant4Props {
  readonly className?: string;
}

/** One colored run of text on a code line. */
interface Span {
  readonly text: string;
  readonly color: string;
}

/** One rendered code line: its gutter number and its colored spans. */
interface CodeLine {
  readonly no: number;
  readonly spans: readonly Span[];
}

const C = {
  delim: nitro.synComment,
  key: nitro.synName,
  value: nitro.text,
  string: nitro.synString,
  fence: nitro.textDim,
  lang: nitro.synField,
  gqlKeyword: nitro.synKeyword,
  gqlField: nitro.synField,
  punct: nitro.synPunct,
} as const;

/**
 * The SKILL.md body, authored span-by-span so the highlighting matches the Nitro
 * editor exactly: YAML frontmatter fenced by `---`, then a fenced GraphQL block
 * with a single operation the agent runs at `/graphql/mcp`.
 */
const LINES: readonly CodeLine[] = [
  { no: 1, spans: [{ text: "---", color: C.delim }] },
  {
    no: 2,
    spans: [
      { text: "name", color: C.key },
      { text: ": ", color: C.punct },
      { text: "search-eshops-catalog", color: C.value },
    ],
  },
  {
    no: 3,
    spans: [
      { text: "description", color: C.key },
      { text: ": ", color: C.punct },
      { text: "Find products via the MCP endpoint", color: C.value },
    ],
  },
  { no: 4, spans: [{ text: "---", color: C.delim }] },
  { no: 5, spans: [{ text: "", color: C.punct }] },
  {
    no: 6,
    spans: [
      { text: "```", color: C.fence },
      { text: "graphql", color: C.lang },
    ],
  },
  {
    no: 7,
    spans: [
      { text: "# POST ", color: C.delim },
      { text: "/graphql/mcp", color: C.delim },
    ],
  },
  {
    no: 8,
    spans: [
      { text: "query", color: C.gqlKeyword },
      { text: " ", color: C.punct },
      { text: "{ ", color: C.punct },
      { text: "searchCatalog", color: C.gqlField },
      { text: "(", color: C.punct },
      { text: "term", color: C.gqlField },
      { text: ": ", color: C.punct },
      { text: '"shoes"', color: C.string },
      { text: ") { ", color: C.punct },
      { text: "id name", color: C.gqlField },
      { text: " } }", color: C.punct },
    ],
  },
  { no: 9, spans: [{ text: "```", color: C.fence }] },
];

const MONO: CSSProperties = {
  fontFamily: nitro.mono,
  fontVariantNumeric: "tabular-nums",
};

/** Small document glyph for the editor title row. */
function FileMark() {
  return (
    <svg
      width={13}
      height={13}
      viewBox="0 0 16 16"
      aria-hidden="true"
      fill="none"
      stroke={nitro.accentHover}
      strokeWidth={1.4}
      strokeLinecap="round"
      strokeLinejoin="round"
      style={{ flex: "none" }}
    >
      <path d="M4 1.75h5L13 5.5v8.75H4z" />
      <path d="M9 1.75V5.5h4" />
    </svg>
  );
}

/**
 * Agentic-coding scene illustration (variant 4): a SKILL.md snippet.
 *
 * A self-contained GitHub-dark code card with a single thin title row and a
 * line-numbered, hand-tokenized body: YAML frontmatter plus one fenced GraphQL
 * operation the agent runs at `/graphql/mcp`. Fully static, the settled final
 * frame.
 */
export function FeedbackVariant4({ className }: FeedbackVariant4Props) {
  return (
    <div
      className={className}
      style={{
        width: "100%",
        maxWidth: 340,
        margin: "0 auto",
        background: nitro.bg,
        border: `1px solid ${nitro.border}`,
        borderRadius: nitro.radius,
        fontFamily: nitro.font,
        color: nitro.text,
        overflow: "hidden",
        userSelect: "none",
        boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
      }}
    >
      {/* Title row: filename + language badge. */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "9px 12px",
          background: nitro.surface,
          borderBottom: `1px solid ${nitro.border}`,
        }}
      >
        <FileMark />
        <span style={{ ...MONO, fontSize: 12, color: nitro.textStrong }}>
          SKILL.md
        </span>
        <span
          style={{
            marginLeft: "auto",
            ...MONO,
            fontSize: 9.5,
            letterSpacing: "0.1em",
            textTransform: "uppercase",
            color: nitro.textSecondary,
            padding: "2px 8px",
            background: nitro.bg,
            border: `1px solid ${nitro.border}`,
            borderRadius: 999,
            whiteSpace: "nowrap",
          }}
        >
          markdown
        </span>
      </div>

      {/* Code body: line-numbered, hand-tokenized SKILL.md. */}
      <div
        role="img"
        aria-label="A SKILL.md file with YAML frontmatter and a fenced GraphQL operation calling /graphql/mcp"
        style={{
          ...MONO,
          fontSize: 11.5,
          lineHeight: "19px",
          padding: "8px 0",
          background: nitro.bg,
          overflowX: "auto",
        }}
      >
        {LINES.map((line) => (
          <div
            key={line.no}
            style={{ display: "flex", whiteSpace: "pre", minHeight: 19 }}
          >
            <span
              aria-hidden="true"
              style={{
                width: 28,
                flex: "none",
                textAlign: "right",
                paddingRight: 12,
                color: nitro.textDim,
              }}
            >
              {line.no}
            </span>
            <span style={{ flex: 1, minWidth: 0 }}>
              {line.spans.map((span, i) => (
                <span key={i} style={{ color: span.color }}>
                  {span.text}
                </span>
              ))}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
