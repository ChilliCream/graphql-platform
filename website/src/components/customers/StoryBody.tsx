"use client";

import React, { FC, ReactNode } from "react";

import type { StorySection } from "@/data/customers/stories";

import { PullQuote } from "./PullQuote";

// Lightweight inline-bold renderer: splits a paragraph on **...** runs and
// promotes the inner text to <strong>. We deliberately keep this minimal
// rather than ship an MDX pipeline for eight stories. If we later need
// links and code blocks, this is the seam to swap for MDX.
function renderParagraph(text: string): ReactNode[] {
  const parts: ReactNode[] = [];
  const regex = /\*\*([^*]+)\*\*/g;
  let lastIndex = 0;
  let match: RegExpExecArray | null;
  let key = 0;
  while ((match = regex.exec(text)) !== null) {
    if (match.index > lastIndex) {
      parts.push(text.slice(lastIndex, match.index));
    }
    parts.push(<strong key={key++}>{match[1]}</strong>);
    lastIndex = match.index + match[0].length;
  }
  if (lastIndex < text.length) {
    parts.push(text.slice(lastIndex));
  }
  return parts;
}

interface StoryBodyProps {
  readonly sections: readonly StorySection[];
}

// Single-column editorial body with bolded inline metrics and pull quotes
// after specific sections. Sticky AtAGlance sidebar lives outside this
// component (see customer-story.tsx) so that the body remains a clean
// reading column on mobile.
export const StoryBody: FC<StoryBodyProps> = ({ sections }) => {
  return (
    <div className="cc-csd-body-main">
      {sections.map((section, idx) => (
        <section key={idx}>
          <h2>{section.heading}</h2>
          {section.paragraphs.map((p, pi) => (
            <p key={pi}>{renderParagraph(p)}</p>
          ))}
          {section.pullQuote ? <PullQuote quote={section.pullQuote} /> : null}
        </section>
      ))}
    </div>
  );
};
