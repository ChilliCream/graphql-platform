import type { CSSProperties, ReactNode } from "react";

/**
 * Agentic coding, v6 bespoke hook: a checked-in `SKILL.md` as the hero.
 *
 * Instead of a diagram or a table, the artifact itself is the illustration: a
 * dark code-editor card rendering a real `SKILL.md`. Governance-violet YAML
 * frontmatter declares the `name` and the `/graphql/mcp` endpoint, a fenced
 * GraphQL snippet shows an agent calling that endpoint, and a coral inline
 * annotation flags `createReview` as a destructive operation with a wavy
 * warning underline. The window chrome (file badge, checked-in path, branch +
 * reviewed status bar) sells the promise that one reviewed file teaches every
 * agent the same thing.
 *
 * Static React Server Component: no hooks, no client APIs, settled final frame.
 * Dark cc-* palette only; status colors (governance violet, destructive coral,
 * approved green) encode real status. Every svg id is prefixed "v6-feedback-5-".
 */

interface FeedbackVariant5Props {
  readonly className?: string;
}

const C = {
  page: "#0b0f1a",
  surface: "#0c1322",
  heading: "#f5f0ea",
  ink: "#a1a3af",
  inkDim: "rgba(245, 241, 234, 0.62)",
  inkFaint: "rgba(245, 241, 234, 0.16)",
  border: "rgba(245, 241, 234, 0.12)",
  navLabel: "#62748e",
  accent: "#5eead4",
  violet: "#8b8ff0",
  violetSoft: "#7c92c6",
  coral: "#f0786a",
  healthy: "#34d399",
  mono: 'ui-monospace, SFMono-Regular, "SF Mono", Menlo, Monaco, Consolas, "Liberation Mono", monospace',
} as const;

/** One syntax-tinted run of text on a code line. */
interface Tok {
  readonly t: string;
  readonly c: string;
  /** wavy coral warning underline, used to flag the destructive call. */
  readonly warn?: boolean;
}

/** One rendered line of the file, with its gutter number and focus state. */
interface Line {
  readonly toks: readonly Tok[];
  /** the destructive line gets a faint coral wash + left accent. */
  readonly focal?: boolean;
}

// The authored SKILL.md, line by line. Violet frontmatter, a fenced GraphQL
// call to /graphql/mcp, and the coral destructive annotation on createReview.
const LINES: readonly Line[] = [
  { toks: [{ t: "---", c: C.violetSoft }] },
  {
    toks: [
      { t: "name", c: C.violet },
      { t: ": ", c: C.inkDim },
      { t: "graphql-tools", c: C.ink },
    ],
  },
  {
    toks: [
      { t: "endpoint", c: C.violet },
      { t: ": ", c: C.inkDim },
      { t: "/graphql/mcp", c: C.accent },
    ],
  },
  { toks: [{ t: "---", c: C.violetSoft }] },
  {
    toks: [
      { t: "```", c: C.navLabel },
      { t: "graphql", c: C.inkDim },
    ],
  },
  {
    toks: [
      { t: "mutation", c: C.accent },
      { t: " {", c: C.inkDim },
      { t: "              ", c: C.inkDim },
      { t: "# ", c: C.navLabel },
      { t: "/graphql/mcp", c: C.accent },
    ],
  },
  {
    focal: true,
    toks: [
      { t: "  ", c: C.inkDim },
      { t: "createReview", c: C.heading, warn: true },
      { t: " { ", c: C.inkDim },
      { t: "id", c: C.ink },
      { t: " }", c: C.inkDim },
      { t: "   ", c: C.inkDim },
      { t: "# destructive", c: C.coral },
    ],
  },
  { toks: [{ t: "}", c: C.inkDim }] },
  { toks: [{ t: "```", c: C.navLabel }] },
];

const MONO_LINE: CSSProperties = {
  fontFamily: C.mono,
  fontSize: 11,
  lineHeight: "19px",
  whiteSpace: "pre",
};

function BranchGlyph() {
  return (
    <svg
      width="11"
      height="11"
      viewBox="0 0 12 12"
      fill="none"
      aria-hidden="true"
      style={{ display: "block", flexShrink: 0 }}
    >
      <circle cx="3.5" cy="2.5" r="1.4" stroke={C.navLabel} strokeWidth="1" />
      <circle cx="3.5" cy="9.5" r="1.4" stroke={C.navLabel} strokeWidth="1" />
      <circle cx="8.5" cy="2.5" r="1.4" stroke={C.navLabel} strokeWidth="1" />
      <path d="M3.5 4 v4" stroke={C.navLabel} strokeWidth="1" />
      <path
        d="M8.5 4 v0.8 a3 3 0 0 1 -3 3 h-2"
        stroke={C.navLabel}
        strokeWidth="1"
        strokeLinecap="round"
      />
    </svg>
  );
}

function ReviewCheck() {
  return (
    <svg
      width="11"
      height="11"
      viewBox="0 0 12 12"
      fill="none"
      aria-hidden="true"
      style={{ display: "block", flexShrink: 0 }}
    >
      <path
        d="M2.5 6.4 L4.8 8.6 L9.5 3.4"
        stroke={C.healthy}
        strokeWidth="1.4"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function StatusItem({ children }: { readonly children: ReactNode }) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 5,
        fontFamily: C.mono,
        fontSize: 9.5,
        color: C.navLabel,
        whiteSpace: "nowrap",
      }}
    >
      {children}
    </span>
  );
}

export function FeedbackVariant5({ className }: FeedbackVariant5Props) {
  return (
    <div
      className={["mx-auto w-full select-none", className ?? ""].join(" ")}
      style={{ maxWidth: 340 }}
    >
      <div
        style={{
          background: C.surface,
          border: `1px solid ${C.border}`,
          borderRadius: 12,
          overflow: "hidden",
          boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
        }}
      >
        {/* window chrome: file badge + name on the left, checked-in path right */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 10,
            padding: "9px 12px",
            borderBottom: `1px solid ${C.border}`,
            background: "rgba(245, 241, 234, 0.02)",
          }}
        >
          <span
            style={{ display: "inline-flex", alignItems: "center", gap: 8 }}
          >
            <span
              style={{
                display: "inline-flex",
                alignItems: "center",
                justifyContent: "center",
                width: 19,
                height: 19,
                borderRadius: 5,
                background: "rgba(139, 143, 240, 0.14)",
                border: `1px solid rgba(139, 143, 240, 0.4)`,
                fontFamily: C.mono,
                fontSize: 8,
                fontWeight: 700,
                letterSpacing: "0.02em",
                color: C.violet,
              }}
            >
              MD
            </span>
            <span style={{ fontFamily: C.mono, fontSize: 12 }}>
              <span style={{ color: C.heading }}>SKILL</span>
              <span style={{ color: C.inkDim }}>.md</span>
            </span>
          </span>
          <span
            style={{
              fontFamily: C.mono,
              fontSize: 9.5,
              color: C.navLabel,
              whiteSpace: "nowrap",
            }}
          >
            .claude/skills/
          </span>
        </div>

        {/* the file body: gutter numbers + syntax-tinted lines */}
        <div style={{ padding: "9px 0", background: C.surface }}>
          {LINES.map((line, i) => (
            <div
              key={`v6-feedback-5-line-${i}`}
              style={{
                display: "flex",
                alignItems: "stretch",
                background: line.focal
                  ? "rgba(240, 120, 106, 0.06)"
                  : "transparent",
                boxShadow: line.focal ? `inset 2px 0 0 ${C.coral}` : undefined,
              }}
            >
              <span
                style={{
                  flexShrink: 0,
                  width: 34,
                  paddingRight: 11,
                  textAlign: "right",
                  borderRight: `1px solid ${C.inkFaint}`,
                  fontFamily: C.mono,
                  fontSize: 10,
                  lineHeight: "19px",
                  color: C.navLabel,
                }}
              >
                {i + 1}
              </span>
              <span style={{ ...MONO_LINE, paddingLeft: 12 }}>
                {line.toks.map((tok, j) => (
                  <span
                    key={`v6-feedback-5-tok-${i}-${j}`}
                    style={
                      tok.warn
                        ? {
                            color: tok.c,
                            textDecorationLine: "underline",
                            textDecorationStyle: "wavy",
                            textDecorationColor: C.coral,
                            textUnderlineOffset: 3,
                          }
                        : { color: tok.c }
                    }
                  >
                    {tok.t}
                  </span>
                ))}
              </span>
            </div>
          ))}
        </div>

        {/* editor status bar: checked-in branch + reviewed-like-code status */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 10,
            padding: "6px 12px",
            borderTop: `1px solid ${C.border}`,
            background: "rgba(245, 241, 234, 0.02)",
          }}
        >
          <span
            style={{ display: "inline-flex", alignItems: "center", gap: 12 }}
          >
            <StatusItem>
              <BranchGlyph />
              main
            </StatusItem>
            <StatusItem>markdown</StatusItem>
          </span>
          <span
            style={{
              display: "inline-flex",
              alignItems: "center",
              gap: 5,
              fontFamily: C.mono,
              fontSize: 9.5,
              color: C.inkDim,
              whiteSpace: "nowrap",
            }}
          >
            <ReviewCheck />
            reviewed
          </span>
        </div>
      </div>
    </div>
  );
}
