"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { AgentTranscriptMock } from "@/components/observability/AgentTranscriptMock";

// MCP-for-agents inverted band. Mirrors Vercel's "Break out of the black box"
// inverted strip on /observability: a value statement headline + sub copy on
// the left, a 3-up of supporting points underneath, then the AgentTranscript
// mock spanning the band. The band itself is rendered by GridSection
// variant="inverted"; this component fills it.

const Outer = styled.div`
  display: flex;
  flex-direction: column;
  gap: clamp(48px, 6vw, 80px);
`;

const HeadingRow = styled.div`
  display: grid;
  grid-template-columns: minmax(0, 1.1fr) minmax(0, 0.9fr);
  gap: 64px;
  align-items: end;

  @media (max-width: 880px) {
    grid-template-columns: 1fr;
    gap: 24px;
    align-items: start;
  }
`;

const Heading = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-weight: 500;
  letter-spacing: -0.025em;
  line-height: 1.05;
  font-size: clamp(32px, 4vw, 56px);
  margin: 0;
  color: #ffffff;
`;

const Sub = styled.p`
  font-size: clamp(15px, 1.1vw, 17px);
  line-height: 1.55;
  color: rgba(255, 255, 255, 0.62);
  margin: 0;
  text-wrap: pretty;
  max-width: 52ch;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, rgba(255, 255, 255, 0.62));
  display: inline-block;
  margin-bottom: 18px;
`;

const PointsRow = styled.div`
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
  border-top: 1px solid rgba(255, 255, 255, 0.12);
  border-bottom: 1px solid rgba(255, 255, 255, 0.12);

  @media (max-width: 880px) {
    grid-template-columns: 1fr;
  }
`;

const Point = styled.div`
  padding: 28px clamp(20px, 2vw, 32px);

  & + & {
    border-left: 1px solid rgba(255, 255, 255, 0.12);
  }

  @media (max-width: 880px) {
    & + & {
      border-left: 0;
      border-top: 1px solid rgba(255, 255, 255, 0.12);
    }
  }

  h3 {
    font-family: var(--cc-font-sans), sans-serif;
    font-weight: 500;
    font-size: 18px;
    letter-spacing: -0.015em;
    line-height: 1.3;
    margin: 0 0 8px;
    color: #ffffff;
  }
  p {
    font-size: 14px;
    line-height: 1.55;
    color: rgba(255, 255, 255, 0.62);
    margin: 0;
    text-wrap: pretty;
  }
`;

const TranscriptShell = styled.div`
  /* Reuses cc-agent-mock styling from ObservabilityGridRoot. The inverted
     band darkens the surface; the transcript inherits the cream ink tokens
     from the root scope. */
`;

const POINTS: readonly { title: string; body: string }[] = [
  {
    title: "Traces over MCP",
    body: "Agents query the same federation traces your team uses. Same data, addressable surface.",
  },
  {
    title: "Schema diffs over MCP",
    body: "Surface every schema change to an agent, with field-level diffs and impact context.",
  },
  {
    title: "Replay queries over MCP",
    body: "Hand a captured prod query to an agent, replay it against staging without a custom pipeline.",
  },
];

export const ObservabilityGridMcpBand: FC = () => {
  return (
    <Outer>
      <HeadingRow>
        <div>
          <Eyebrow>MCP for agents</Eyebrow>
          <Heading>Break out of the black box.</Heading>
        </div>
        <Sub>
          The same observability data, exposed through Nitro&apos;s MCP surface.
          Traces, schema diffs, and replay are queryable by any MCP-aware agent.
        </Sub>
      </HeadingRow>
      <PointsRow>
        {POINTS.map((p) => (
          <Point key={p.title}>
            <h3>{p.title}</h3>
            <p>{p.body}</p>
          </Point>
        ))}
      </PointsRow>
      <TranscriptShell>
        <AgentTranscriptMock />
      </TranscriptShell>
    </Outer>
  );
};
