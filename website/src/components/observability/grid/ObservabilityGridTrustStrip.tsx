"use client";

import React, { FC } from "react";
import styled from "styled-components";

// Trust strip: 3 text-only cells separated by 1px hairlines, no card chrome.
// Sits between feature panels as a typographic beat. Each cell is a small
// h3 + body paragraph; no icons, no boxes, just the band carrying the rhythm.

const Outer = styled.div`
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 0;
  width: 100%;

  @media (max-width: 880px) {
    grid-template-columns: 1fr;
  }
`;

const Cell = styled.div`
  padding: clamp(20px, 2vw, 32px) clamp(20px, 2.5vw, 36px);

  & + & {
    border-left: 1px solid var(--cc-grid-hairline);
  }

  @media (max-width: 880px) {
    & + & {
      border-left: 0;
      border-top: 1px solid var(--cc-grid-hairline);
    }
  }
`;

const Title = styled.h3`
  font-family: var(--cc-font-sans), sans-serif;
  font-weight: 500;
  font-size: 18px;
  letter-spacing: -0.015em;
  line-height: 1.3;
  margin: 0 0 8px;
  color: var(--cc-ink);
`;

const Body = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: var(--cc-ink-dim);
  margin: 0;
  text-wrap: pretty;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, var(--cc-ink-dim));
  display: inline-block;
  margin-bottom: 12px;
`;

const ITEMS: readonly { eyebrow: string; title: string; body: string }[] = [
  {
    eyebrow: "Federation",
    title: "Federation-aware out of the box",
    body: "Traces, errors, and replays know about the gateway and every owning service. Nothing to wire up.",
  },
  {
    eyebrow: "Environments",
    title: "Same surface for dev, staging, prod",
    body: "One control surface across every environment. Capture in prod, replay in staging, ship the fix the same day.",
  },
  {
    eyebrow: "OTEL",
    title: "OpenTelemetry. Bring your backend.",
    body: "Federation traces drop into Jaeger, Tempo, Datadog, Honeycomb, Grafana, New Relic. No glue code, no proxies.",
  },
];

export const ObservabilityGridTrustStrip: FC = () => {
  return (
    <Outer>
      {ITEMS.map((item) => (
        <Cell key={item.eyebrow}>
          <Eyebrow>{item.eyebrow}</Eyebrow>
          <Title>{item.title}</Title>
          <Body>{item.body}</Body>
        </Cell>
      ))}
    </Outer>
  );
};
