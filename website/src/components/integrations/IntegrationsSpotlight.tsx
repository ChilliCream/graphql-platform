"use client";

import Link from "next/link";
import React, { FC } from "react";

import { IDE_CLIENTS } from "@/data/agents/ide-clients";

// Single-letter monogram tile. Mirrors the agents/WorksWhereYouWork
// vocabulary so the four MCP-compatible clients render identically wherever
// they appear on the site.
const Monogram: FC<{ letter: string }> = ({ letter }) => (
  <svg viewBox="0 0 32 32" width="32" height="32" aria-hidden>
    <text
      x="16"
      y="22"
      textAnchor="middle"
      fontFamily="var(--cc-font-sans), sans-serif"
      fontWeight={500}
      fontSize="15"
      fill="currentColor"
    >
      {letter}
    </text>
  </svg>
);

// Section 02: rotating editorial slot. Launch theme is "Build for agents" so
// the page leads with a story, not a phonebook. The big inset card carries
// an eyebrow, a headline, a one-line pitch, the four MCP-compatible IDE
// clients as a monogram strip, and a CTA into the AI & Agents category. When
// marketing wants a different campaign they swap this component, not the
// page; same pattern Vercel uses for their rotating Marketplace hero.
export const IntegrationsSpotlight: FC = () => {
  return (
    <section className="cc-in-section cc-in-spotlight">
      <div className="cc-section-label">
        <span className="num">02</span> Spotlight
      </div>
      <div className="cc-in-spotlight-inner">
        <span className="cc-in-spotlight-eyebrow">Build for agents</span>
        <h2 className="display">Your platform, ready for any LLM.</h2>
        <p>
          Expose your Hot Chocolate schema as an MCP server and Claude, Cursor,
          Copilot Chat, and GitHub Copilot can introspect, query, and mutate
          with the same authorization as your users. Same schema, two audiences.
        </p>
        <div className="cc-in-spotlight-clients">
          {IDE_CLIENTS.map((c) => (
            <a
              key={c.key}
              href={c.setup}
              className="cc-in-spotlight-client"
              rel="noopener"
            >
              <span className="cc-in-spotlight-client-mono">
                <Monogram letter={c.letter} />
              </span>
              <span className="cc-in-spotlight-client-name">{c.name}</span>
            </a>
          ))}
        </div>
        <Link
          href="/integrations?category=ai-agents"
          className="cc-in-spotlight-cta"
        >
          See agent integrations →
        </Link>
      </div>
    </section>
  );
};
