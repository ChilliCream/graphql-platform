"use client";

import Link from "next/link";
import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { ActLabel } from "@/components/redesign-system/cinematic";
import { Orbit, RayBurst } from "@/components/redesign-system/illustrations";
import { IDE_CLIENTS } from "@/data/agents/ide-clients";

// MCP-compatible IDE clients along the inner ring; the outer ring carries the
// model providers that route through MCP. Both render as quiet labels orbiting
// the central MCP endpoint, which is the marketplace's headline pitch.
const MODEL_PROVIDERS = [
  { key: "openai", letter: "O", name: "OpenAI" },
  { key: "anthropic", letter: "A", name: "Anthropic" },
  { key: "xai", letter: "X", name: "xAI" },
  { key: "gemini", letter: "G", name: "Gemini" },
] as const;

const BurstMonogram: FC<{ letter: string }> = ({ letter }) => (
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

// Cinematic spotlight: same content as IntegrationsSpotlight; the band carries
// the cinematic `cc-band` class so IntegrationsCinematicRoot can apply the
// extended top gutter, and the in-section `.cc-section-label` is hidden by
// the cinematic root (the `<ActLabel>` is mounted at the band level by
// IntegrationsCinematic).
export const IntegrationsCinematicSpotlight: FC = () => {
  return (
    <Band
      variant="accent"
      className="cc-band cc-band-spotlight"
      ariaLabel="Build for agents"
    >
      <ActLabel n="02" name="Spotlight" />
      <div className="cc-in-spotlight-grid">
        <div className="cc-in-spotlight-copy">
          <span className="cc-in-spotlight-eyebrow">Build for agents</span>
          <h2 className="display">Your platform, ready for any LLM.</h2>
          <p>
            Expose your Hot Chocolate schema as an MCP server and Claude,
            Cursor, Copilot Chat, and GitHub Copilot can introspect, query, and
            mutate with the same authorization as your users. Same schema, two
            audiences.
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
                  <BurstMonogram letter={c.letter} />
                </span>
                <span className="cc-in-spotlight-client-name">{c.name}</span>
              </a>
            ))}
          </div>
          <Link
            href="/integrations?category=ai-agents&v=cinematic"
            className="cc-in-spotlight-cta"
          >
            See agent integrations →
          </Link>
        </div>
        <div className="cc-in-spotlight-orbital" aria-hidden>
          <div className="cc-in-spotlight-orbital-bg">
            <RayBurst rayCount={20} />
            <Orbit rings={3} rotate={-12} />
          </div>
          <div className="cc-in-spotlight-orbital-marks">
            {MODEL_PROVIDERS.map((p, i) => {
              const angle = (i * 360) / MODEL_PROVIDERS.length - 90;
              const rad = (angle * Math.PI) / 180;
              const radius = 42;
              const x = 50 + Math.cos(rad) * radius;
              const y = 50 + Math.sin(rad) * radius;
              return (
                <span
                  key={p.key}
                  className="cc-in-spotlight-orbital-mark"
                  style={{ left: `${x}%`, top: `${y}%` }}
                >
                  <span className="cc-in-spotlight-orbital-mono">
                    {p.letter}
                  </span>
                  <span className="cc-in-spotlight-orbital-name">{p.name}</span>
                </span>
              );
            })}
            <span className="cc-in-spotlight-orbital-core">MCP</span>
          </div>
        </div>
      </div>
    </Band>
  );
};
