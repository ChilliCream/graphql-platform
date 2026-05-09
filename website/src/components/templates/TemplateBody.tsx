"use client";

import React, { FC } from "react";

import type { TemplateSection } from "@/data/templates/templates";

import { CodeBlock } from "./CodeBlock";

interface TemplateBodyProps {
  readonly sections: readonly TemplateSection[];
}

// Long-form body: section heading + paragraphs + optional code block. We
// deliberately keep the markup minimal-but-real-feeling rather than wire
// an MDX pipeline for eight templates — once template repos publish real
// READMEs we'll swap this for a remote-MDX renderer keyed by the same
// section schema.
export const TemplateBody: FC<TemplateBodyProps> = ({ sections }) => {
  return (
    <div className="cc-tpd-body-main">
      {sections.map((section, idx) => (
        <section key={idx}>
          <h2>{section.heading}</h2>
          {section.paragraphs.map((p, pi) => (
            <p key={pi}>{p}</p>
          ))}
          {section.code ? (
            <CodeBlock
              language={section.code.language}
              code={section.code.code}
            />
          ) : null}
        </section>
      ))}
    </div>
  );
};
