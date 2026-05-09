"use client";

import React, { FC, ReactNode } from "react";

import { Band } from "@/components/redesign-system/Band";
import type { CodeSnippet as CodeSnippetData } from "@/data/solutions/types";

interface CodeSnippetProps {
  readonly snippet: CodeSnippetData;
  readonly stepNumber: string;
}

// Section 05: 12-20 line code block. Industry pages omit this section
// entirely; the renderer simply doesn't include it. The frame mirrors a
// VS Code editor: file-name pill on the left, language badge on the
// right, monospace body with light syntax tinting.
//
// We do not pull in a full Prism runtime here. Instead, the snippet is
// tokenized line-by-line with a small, language-aware regex pass:
// keywords/types in violet, strings in amber, comments dim italic, keys
// in cool blue. Good enough for a 15-line marketing snippet, zero
// dependencies, no hydration cost.

interface TokenSpec {
  readonly className: string;
  readonly pattern: RegExp;
}

const COMMON: readonly TokenSpec[] = [
  { className: "tok-string", pattern: /"(?:\\.|[^"\\])*"/g },
  { className: "tok-string", pattern: /'(?:\\.|[^'\\])*'/g },
];

const LANGUAGE_TOKENS: Record<string, readonly TokenSpec[]> = {
  csharp: [
    {
      className: "tok-keyword",
      pattern:
        /\b(var|public|sealed|record|class|interface|namespace|using|new|return|await|async|true|false|null|this|if|else|for|foreach|while|throw|catch|try|in|out|ref|readonly|static|const|void|int|string|bool|double|float|long|short|byte|object|Task|Guid|CancellationToken|var)\b/g,
    },
    { className: "tok-attr", pattern: /\[\w[\w.]*\]/g },
    { className: "tok-comment", pattern: /\/\/[^\n]*/g },
  ],
  typescript: [
    {
      className: "tok-keyword",
      pattern:
        /\b(const|let|var|function|return|await|async|true|false|null|undefined|if|else|for|while|new|class|interface|type|export|import|from|extends|implements)\b/g,
    },
    { className: "tok-comment", pattern: /\/\/[^\n]*/g },
    { className: "tok-key", pattern: /\b[A-Za-z_]\w*(?=:)/g },
  ],
  yaml: [
    { className: "tok-comment", pattern: /#[^\n]*/g },
    { className: "tok-key", pattern: /^\s*[A-Za-z_][\w-]*(?=:)/gm },
  ],
  json: [
    { className: "tok-key", pattern: /"[^"]+"(?=\s*:)/g },
    { className: "tok-comment", pattern: /\/\/[^\n]*/g },
  ],
};

interface TokenRange {
  readonly start: number;
  readonly end: number;
  readonly className: string;
}

// Tokenize a single line. Common token specs (strings) win first so that
// keywords inside string literals don't get re-highlighted.
const tokenize = (line: string, language: string): readonly TokenRange[] => {
  const specs = [...COMMON, ...(LANGUAGE_TOKENS[language] ?? [])];
  const ranges: TokenRange[] = [];

  for (const spec of specs) {
    const re = new RegExp(spec.pattern.source, spec.pattern.flags);
    let match: RegExpExecArray | null;
    while ((match = re.exec(line)) !== null) {
      const start = match.index;
      const end = start + match[0].length;
      // Reject overlap with an earlier (higher priority) range.
      const overlaps = ranges.some((r) => !(end <= r.start || start >= r.end));
      if (!overlaps) {
        ranges.push({ start, end, className: spec.className });
      }
    }
  }

  return ranges.sort((a, b) => a.start - b.start);
};

const renderLine = (line: string, language: string, key: number): ReactNode => {
  const ranges = tokenize(line, language);
  if (ranges.length === 0) {
    return (
      <span key={key}>
        {line || " "}
        {"\n"}
      </span>
    );
  }
  const out: ReactNode[] = [];
  let cursor = 0;
  ranges.forEach((r, i) => {
    if (cursor < r.start) {
      out.push(line.slice(cursor, r.start));
    }
    out.push(
      <span key={`${key}-${i}`} className={r.className}>
        {line.slice(r.start, r.end)}
      </span>
    );
    cursor = r.end;
  });
  if (cursor < line.length) {
    out.push(line.slice(cursor));
  }
  return (
    <span key={key}>
      {out}
      {"\n"}
    </span>
  );
};

export const CodeSnippet: FC<CodeSnippetProps> = ({ snippet, stepNumber }) => {
  const { language, fileName, source } = snippet;
  // strip a single leading newline so the snippet starts at column 1
  const cleaned = source.startsWith("\n") ? source.slice(1) : source;
  const lines = cleaned.replace(/\n$/, "").split("\n");

  return (
    <Band variant="tinted" ariaLabel="Snippet">
      <div className="cc-sl-section cc-sl-code">
        <div className="cc-section-label">
          <span className="num">{stepNumber}</span> Snippet
        </div>
        <div className="cc-sl-code-inner">
          <div className="cc-sl-code-frame">
            <div className="cc-sl-code-head">
              <span className="cc-sl-code-file">{fileName}</span>
              <span className="cc-sl-code-lang">{language}</span>
            </div>
            <pre className="cc-sl-code-body">
              <code>
                {lines.map((line, i) => renderLine(line, language, i))}
              </code>
            </pre>
          </div>
        </div>
      </div>
    </Band>
  );
};
