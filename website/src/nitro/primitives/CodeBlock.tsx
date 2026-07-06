/**
 * CodeBlock — a hand-tokenized, syntax-highlighted code view with a progressive reveal that
 * reads as live typing (request editor) or streaming (response/JSON). Tokenizes GraphQL or
 * JSON using the exact Nitro editor token colors. Reveals by character count driven by
 * a local `progress` MotionValue + play window; with no `progress` it renders fully (static).
 *
 * Not a real editor — a presentational clone for the reel. Monospace (Fira Code), optional
 * left line-number gutter, optional caret at the typing boundary.
 */
import { useMemo, useState } from "react";
import type { CSSProperties } from "react";
import {
  useMotionValue,
  useMotionValueEvent,
  useTransform,
  type MotionValue,
} from "motion/react";
import { token } from "../lib/tokens";

type Tok = { t: string; c: string };

const GQL_KEYWORDS = new Set([
  "query",
  "mutation",
  "subscription",
  "fragment",
  "on",
  "type",
  "input",
  "enum",
  "interface",
  "union",
  "scalar",
  "schema",
  "directive",
  "implements",
  "extend",
  "true",
  "false",
  "null",
]);

// SQL keywords (upper or lower) for the Span Details SQL statement highlighting.
const SQL_KEYWORDS = new Set([
  "select",
  "from",
  "where",
  "and",
  "or",
  "not",
  "in",
  "is",
  "null",
  "as",
  "on",
  "join",
  "inner",
  "left",
  "right",
  "outer",
  "full",
  "group",
  "by",
  "order",
  "having",
  "limit",
  "offset",
  "asc",
  "desc",
  "distinct",
  "insert",
  "into",
  "values",
  "update",
  "set",
  "delete",
  "true",
  "false",
  "between",
  "like",
  "count",
  "sum",
  "avg",
  "min",
  "max",
  "case",
  "when",
  "then",
  "else",
  "end",
]);

/** Tokenize one line of SQL into colored runs. */
function tokenizeSql(line: string): Tok[] {
  const out: Tok[] = [];
  const re =
    /(\s+)|(--.*)|('(?:[^'\\]|\\.)*')|(\$\d+|:[A-Za-z0-9_]+)|(-?\d+(?:\.\d+)?)|([(),.;*=<>!+/-]|<=|>=)|([A-Za-z_][A-Za-z0-9_]*)/g;
  let m: RegExpExecArray | null;
  let last = 0;
  while ((m = re.exec(line))) {
    if (m.index > last)
      out.push({ t: line.slice(last, m.index), c: token.synPunct });
    last = re.lastIndex;
    if (m[1]) out.push({ t: m[1], c: token.synPunct });
    else if (m[2]) out.push({ t: m[2], c: token.synComment });
    else if (m[3]) out.push({ t: m[3], c: token.synString });
    else if (m[4])
      out.push({ t: m[4], c: token.blue }); // bind params $1 / :name
    else if (m[5])
      out.push({ t: m[5], c: token.synString }); // numbers
    else if (m[6]) out.push({ t: m[6], c: token.synPunct });
    else if (m[7]) {
      const w = m[7];
      const c = SQL_KEYWORDS.has(w.toLowerCase())
        ? token.synKeyword
        : token.synField;
      out.push({ t: w, c });
    }
  }
  if (last < line.length) out.push({ t: line.slice(last), c: token.synPunct });
  return out;
}

/** Tokenize one line of GraphQL into colored runs (no cross-line tokens in our data). */
function tokenizeGraphql(line: string): Tok[] {
  const out: Tok[] = [];
  const re =
    /(\s+)|(#.*)|("(?:[^"\\]|\\.)*")|(\$[A-Za-z0-9_]+)|(@[A-Za-z0-9_]+)|(-?\d+(?:\.\d+)?)|([{}()[\]:!=,|&])|([A-Za-z_][A-Za-z0-9_]*)/g;
  let m: RegExpExecArray | null;
  let last = 0;
  while ((m = re.exec(line))) {
    if (m.index > last)
      out.push({ t: line.slice(last, m.index), c: token.synPunct });
    last = re.lastIndex;
    if (m[1]) out.push({ t: m[1], c: token.synPunct });
    else if (m[2]) out.push({ t: m[2], c: token.synComment });
    else if (m[3]) out.push({ t: m[3], c: token.synString });
    else if (m[4]) out.push({ t: m[4], c: token.blue });
    else if (m[5]) out.push({ t: m[5], c: token.synKeyword });
    else if (m[6]) out.push({ t: m[6], c: token.synString });
    else if (m[7]) out.push({ t: m[7], c: token.synPunct });
    else if (m[8]) {
      const w = m[8];
      const c = GQL_KEYWORDS.has(w)
        ? token.synKeyword
        : /^[A-Z]/.test(w)
          ? token.synType
          : token.synField;
      out.push({ t: w, c });
    }
  }
  if (last < line.length) out.push({ t: line.slice(last), c: token.synPunct });
  return out;
}

/** Tokenize one line of JSON; a string immediately followed by ':' is a key (green). */
function tokenizeJson(line: string): Tok[] {
  const out: Tok[] = [];
  const re =
    /(\s+)|("(?:[^"\\]|\\.)*")(\s*:)?|(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)|(true|false|null)|([{}[\],:])/g;
  let m: RegExpExecArray | null;
  let last = 0;
  while ((m = re.exec(line))) {
    if (m.index > last)
      out.push({ t: line.slice(last, m.index), c: token.synPunct });
    last = re.lastIndex;
    if (m[1]) out.push({ t: m[1], c: token.synPunct });
    else if (m[2]) {
      const isKey = !!m[3];
      out.push({ t: m[2], c: isKey ? token.synName : token.synString });
      if (m[3]) out.push({ t: m[3], c: token.synPunct });
    } else if (m[4]) out.push({ t: m[4], c: token.synString });
    else if (m[5]) out.push({ t: m[5], c: token.synKeyword });
    else if (m[6]) out.push({ t: m[6], c: token.synPunct });
  }
  if (last < line.length) out.push({ t: line.slice(last), c: token.synPunct });
  return out;
}

export interface CodeBlockProps {
  code: string;
  lang: "graphql" | "json" | "sql";
  /** local progress driving the reveal; omit for fully-static (final) rendering */
  progress?: MotionValue<number>;
  /** when within `progress` this types in, as [start,end] fractions */
  playWindow?: [number, number];
  gutter?: boolean;
  caret?: boolean;
  fontSize?: number;
  lineHeight?: number;
  /** start line number (gutter) */
  startLine?: number;
  padding?: number;
  style?: CSSProperties;
  ariaLabel?: string;
}

export function CodeBlock({
  code,
  lang,
  progress,
  playWindow = [0, 1],
  gutter = true,
  caret = true,
  fontSize = 12.5,
  lineHeight = 19,
  startLine = 1,
  padding = 10,
  style,
  ariaLabel,
}: CodeBlockProps) {
  const lines = useMemo(() => code.split("\n"), [code]);
  const tokenized = useMemo(
    () =>
      lines.map((l) =>
        lang === "graphql"
          ? tokenizeGraphql(l)
          : lang === "sql"
            ? tokenizeSql(l)
            : tokenizeJson(l),
      ),
    [lines, lang],
  );
  // char start offset of each line (newline counts as 1)
  const starts = useMemo(() => {
    const s: number[] = [];
    let acc = 0;
    for (const l of lines) {
      s.push(acc);
      acc += l.length + 1;
    }
    return s;
  }, [lines]);
  const totalChars = code.length;

  const [w0, w1] = playWindow;
  const fallback = useMotionValue(1);
  const reveal = useTransform(
    progress ?? fallback,
    [w0, Math.max(w0 + 0.001, w1), 1],
    [0, 1, 1],
    {
      clamp: true,
    },
  );
  const [shown, setShown] = useState(() =>
    progress ? Math.round(reveal.get() * totalChars) : totalChars,
  );
  useMotionValueEvent(reveal, "change", (v) => {
    const n = Math.round(v * totalChars);
    setShown((prev) => (prev === n ? prev : n));
  });
  const shownChars = progress ? shown : totalChars;

  const gutterW = gutter
    ? Math.max(22, String(startLine + lines.length).length * 8 + 14)
    : 0;

  return (
    <div
      role="img"
      aria-label={ariaLabel ?? `${lang} code`}
      style={{
        fontFamily: token.mono,
        fontSize,
        lineHeight: `${lineHeight}px`,
        padding: `${padding}px 0`,
        whiteSpace: "pre",
        overflow: "hidden",
        ...style,
      }}
    >
      {lines.map((line, i) => {
        const s = starts[i];
        if (shownChars <= s && i > 0) return null; // line not started yet
        const vis = Math.max(0, Math.min(line.length, shownChars - s));
        const isCurrent = shownChars > s && shownChars <= s + line.length;
        return (
          <div key={i} style={{ display: "flex", minHeight: lineHeight }}>
            {gutter && (
              <span
                style={{
                  width: gutterW,
                  flex: "0 0 auto",
                  textAlign: "right",
                  paddingRight: 12,
                  color: token.textDim,
                  userSelect: "none",
                }}
              >
                {startLine + i}
              </span>
            )}
            <span style={{ flex: 1, minWidth: 0 }}>
              <Line toks={tokenized[i]} vis={vis} />
              {caret && isCurrent && shownChars < totalChars && (
                <span
                  style={{
                    display: "inline-block",
                    width: 1.5,
                    height: fontSize,
                    transform: "translateY(2px)",
                    background: token.textStrong,
                    marginLeft: 1,
                  }}
                />
              )}
            </span>
          </div>
        );
      })}
    </div>
  );
}

function Line({ toks, vis }: { toks: Tok[]; vis: number }) {
  let used = 0;
  const out: React.ReactNode[] = [];
  for (let i = 0; i < toks.length; i++) {
    const tk = toks[i];
    if (used >= vis) break;
    const take = Math.min(tk.t.length, vis - used);
    const text = tk.t.slice(0, take);
    out.push(
      <span key={i} style={{ color: tk.c }}>
        {text}
      </span>,
    );
    used += tk.t.length;
  }
  return <>{out}</>;
}
