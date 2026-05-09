"use client";

import Link from "next/link";
import React, { FC } from "react";
import styled from "styled-components";

import {
  GRID_TOKENS,
  GridSection,
  GridSplit,
} from "@/components/redesign-system/grid";
import { IDE_CLIENTS } from "@/data/agents/ide-clients";

// MCP-compatible model providers along the orbital ring; the inner ring
// carries the IDE clients that route through MCP. Same data the Default and
// Cinematic spotlights use, rendered here as a compact schematic diagram on
// the right of the GridSplit so the band reads as drafting paper rather than
// a glow card.
const MODEL_PROVIDERS = [
  { key: "openai", letter: "O", name: "OpenAI" },
  { key: "anthropic", letter: "A", name: "Anthropic" },
  { key: "xai", letter: "X", name: "xAI" },
  { key: "gemini", letter: "G", name: "Gemini" },
] as const;

// Archetype B with the spotlight payload: GridSplit 60-40, copy on the left
// and a small inline orbital diagram on the right. No card chrome (the panes
// are GridSplit cells; the band is the surface, in the Vercel discipline).
export const IntegrationsGridSpotlight: FC = () => {
  return (
    <GridSection variant="default" hairlineBottom aria-label="Build for agents">
      <Split>
        <GridSplit ratio="60-40">
          <CopyPane>
            <Eyebrow>Build for agents</Eyebrow>
            <Headline>Your platform, ready for any LLM.</Headline>
            <Body>
              Expose your Hot Chocolate schema as an MCP server and Claude,
              Cursor, Copilot Chat, and GitHub Copilot can introspect, query,
              and mutate with the same authorization as your users. Same schema,
              two audiences.
            </Body>
            <Clients>
              {IDE_CLIENTS.map((c) => (
                <ClientLink key={c.key} href={c.setup} rel="noopener">
                  <ClientMono>{c.letter}</ClientMono>
                  <ClientName>{c.name}</ClientName>
                </ClientLink>
              ))}
            </Clients>
            <CtaLink href="/integrations?category=ai-agents">
              See agent integrations <span aria-hidden>&rarr;</span>
            </CtaLink>
          </CopyPane>
          <DiagramPane aria-hidden>
            <Diagram>
              <Ring $size={92} />
              <Ring $size={66} />
              <Ring $size={42} />
              {MODEL_PROVIDERS.map((p, i) => {
                const angle = (i * 360) / MODEL_PROVIDERS.length - 90;
                const rad = (angle * Math.PI) / 180;
                const radius = 44;
                const x = 50 + Math.cos(rad) * radius;
                const y = 50 + Math.sin(rad) * radius;
                return (
                  <Node key={p.key} style={{ left: `${x}%`, top: `${y}%` }}>
                    <NodeMono>{p.letter}</NodeMono>
                    <NodeName>{p.name}</NodeName>
                  </Node>
                );
              })}
              <Core>MCP</Core>
            </Diagram>
          </DiagramPane>
        </GridSplit>
      </Split>
    </GridSection>
  );
};

const Split = styled.div`
  max-width: 100%;
`;

const CopyPane = styled.div`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  padding: clamp(32px, 5vw, 56px);
  gap: 18px;
`;

const DiagramPane = styled.div`
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: clamp(32px, 5vw, 48px);
  min-height: 360px;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const Headline = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: ${GRID_TOKENS.h2Size};
  font-weight: 600;
  line-height: 1.05;
  letter-spacing: -0.025em;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  max-width: 22ch;
  text-wrap: balance;
`;

const Body = styled.p`
  font-size: clamp(15px, 1.1vw, 17px);
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  max-width: 56ch;
  text-wrap: pretty;
`;

const Clients = styled.div`
  display: flex;
  flex-wrap: wrap;
  gap: 0;
  margin-top: 6px;
  border: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
`;

const ClientLink = styled.a`
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  background: transparent;
  color: ${GRID_TOKENS.inkPrimary};
  text-decoration: none;
  transition: background 0.12s ease;

  & + & {
    border-left: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  }

  &:hover {
    background: var(--cc-grid-card-hover, ${GRID_TOKENS.bgHover});
  }
`;

const ClientMono = styled.span`
  width: 22px;
  height: 22px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  font-family: var(--cc-font-sans), sans-serif;
  font-weight: 600;
  font-size: 12px;
  line-height: 1;
`;

const ClientName = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 13px;
  font-weight: 500;
  letter-spacing: -0.005em;
`;

const CtaLink = styled(Link)`
  display: inline-flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  text-decoration: none;

  &:hover {
    text-decoration: underline;
    text-underline-offset: 4px;
  }
`;

const Diagram = styled.div`
  position: relative;
  width: clamp(240px, 26vw, 340px);
  aspect-ratio: 1;
  color: ${GRID_TOKENS.inkPrimary};
`;

interface RingProps {
  $size: number;
}

const Ring = styled.div<RingProps>`
  position: absolute;
  left: 50%;
  top: 50%;
  width: ${({ $size }) => `${$size}%`};
  height: ${({ $size }) => `${$size}%`};
  transform: translate(-50%, -50%);
  border: 1px solid
    var(--cc-grid-hairline-strong, ${GRID_TOKENS.hairlineStrong});
  border-radius: 50%;
  pointer-events: none;
`;

const Node = styled.span`
  position: absolute;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transform: translate(-50%, -50%);
  padding: 5px 9px 5px 5px;
  background: var(--cc-grid-card-bg, ${GRID_TOKENS.bgCard});
  border: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 11px;
  white-space: nowrap;
`;

const NodeMono = styled.span`
  width: 18px;
  height: 18px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: var(--cc-accent, #ffffff);
  color: ${GRID_TOKENS.bgBase};
  font-weight: 700;
  font-size: 11px;
  line-height: 1;
`;

const NodeName = styled.span`
  font-weight: 500;
  letter-spacing: -0.005em;
`;

const Core = styled.span`
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  width: 56px;
  height: 56px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  color: ${GRID_TOKENS.bgBase};
  font-family: var(--cc-font-mono), monospace;
  font-size: 13px;
  font-weight: 600;
  letter-spacing: 0.08em;
`;
