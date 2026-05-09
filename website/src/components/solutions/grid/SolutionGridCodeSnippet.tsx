"use client";

import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import type { CodeSnippet } from "@/data/solutions/types";

interface SolutionGridCodeSnippetProps {
  readonly snippet: CodeSnippet;
}

// Archetype G (code-on-glass demo). The existing default-variant code
// snippet (CodeSnippet.tsx) bakes its own rounded frame. Here we re-render
// the same data inside a noPadding GridCard so the chrome reads as the
// Grid's hairline frame: square corners, header row separated by a 1px
// internal hairline, monospace body inset against the card surface.
//
// We keep the language-aware tokenizer inline so the syntax tints match the
// default variant exactly without depending on a runtime highlighter.

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

const tokenize = (line: string, language: string): readonly TokenRange[] => {
  const specs = [...COMMON, ...(LANGUAGE_TOKENS[language] ?? [])];
  const ranges: TokenRange[] = [];

  for (const spec of specs) {
    const re = new RegExp(spec.pattern.source, spec.pattern.flags);
    let match: RegExpExecArray | null;
    while ((match = re.exec(line)) !== null) {
      const start = match.index;
      const end = start + match[0].length;
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
        {line || " "}
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

export const SolutionGridCodeSnippet: FC<SolutionGridCodeSnippetProps> = ({
  snippet,
}) => {
  const { language, fileName, source } = snippet;
  const cleaned = source.startsWith("\n") ? source.slice(1) : source;
  const lines = cleaned.replace(/\n$/, "").split("\n");

  return (
    <GridSection hairlineBottom>
      <Wrap>
        <GridCard noPadding>
          <Head>
            <File>{fileName}</File>
            <Lang>{language}</Lang>
          </Head>
          <Body>
            <code>{lines.map((line, i) => renderLine(line, language, i))}</code>
          </Body>
        </GridCard>
      </Wrap>
    </GridSection>
  );
};

const Wrap = styled.div`
  max-width: 1080px;
  margin: 0 auto;
`;

const Head = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 20px;
  border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
`;

const File = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  color: ${GRID_TOKENS.inkPrimary};
  letter-spacing: 0.04em;
`;

const Lang = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
  padding: 4px 8px;
  border: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
`;

const Body = styled.pre`
  margin: 0;
  padding: 22px 24px;
  overflow-x: auto;
  font-family: var(--cc-font-mono), monospace;
  font-size: 13.5px;
  line-height: 1.65;
  color: ${GRID_TOKENS.inkPrimary};
  white-space: pre;

  .tok-comment {
    color: ${GRID_TOKENS.inkMuted};
    font-style: italic;
  }
  .tok-keyword {
    color: var(--cc-col-usr, oklch(0.72 0.18 310));
  }
  .tok-string {
    color: var(--cc-col-bil, oklch(0.82 0.16 90));
  }
  .tok-number {
    color: var(--cc-col-ord, oklch(0.76 0.16 150));
  }
  .tok-key {
    color: var(--cc-col-shi, oklch(0.74 0.14 220));
  }
  .tok-type {
    color: var(--cc-col-cat, oklch(0.74 0.18 30));
  }
  .tok-attr {
    color: var(--cc-amber, oklch(0.85 0.16 75));
  }
`;
