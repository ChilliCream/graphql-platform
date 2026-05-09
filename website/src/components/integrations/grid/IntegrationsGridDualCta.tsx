"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GRID_TOKENS,
  GridSection,
  GridSplit,
} from "@/components/redesign-system/grid";

// Archetype L. Two CTAs side by side, mirroring the Default and Cinematic
// dual-CTA closer. Two audiences read this page: developers wishing for a
// missing integration, and authors looking to ship one. Both paths sit on the
// closing band as content panels separated by a single 1px hairline rule, no
// boxes inside boxes.
export const IntegrationsGridDualCta: FC = () => {
  return (
    <GridSection
      variant="default"
      hairlineTop
      hairlineBottom
      aria-label="Help shape the marketplace"
    >
      <GridSplit ratio="50-50">
        <Pane
          as="a"
          href="https://github.com/ChilliCream/graphql-platform/issues/new?labels=integration-request"
          rel="noopener"
        >
          <Eyebrow>Don&apos;t see your tool?</Eyebrow>
          <Title>File an integration request.</Title>
          <Body>
            Tell us which auth provider, observability backend, or messaging
            transport is missing. The popular ones become native packages.
          </Body>
          <Cta>
            Open an issue <span aria-hidden>&rarr;</span>
          </Cta>
        </Pane>
        <Pane
          as="a"
          href="https://chillicream.com/docs/hotchocolate/v14/server"
          rel="noopener"
        >
          <Eyebrow>Building on the platform?</Eyebrow>
          <Title>Build an integration.</Title>
          <Body>
            The integration docs walk through the package layout, the testing
            harness, and the PR shape we use to add a community listing here.
          </Body>
          <Cta>
            Read the docs <span aria-hidden>&rarr;</span>
          </Cta>
        </Pane>
      </GridSplit>
    </GridSection>
  );
};

const Pane = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: clamp(36px, 5vw, 64px);
  background: var(--cc-grid-card-bg, ${GRID_TOKENS.bgCard});
  color: ${GRID_TOKENS.inkPrimary};
  text-decoration: none;
  transition: background 0.12s ease;
  min-height: 280px;

  &:hover {
    background: var(--cc-grid-card-hover, ${GRID_TOKENS.bgHover});
  }
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const Title = styled.h3`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(22px, 2.4vw, 30px);
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.15;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
`;

const Body = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  flex: 1;
  text-wrap: pretty;
  max-width: 52ch;
`;

const Cta = styled.span`
  margin-top: auto;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  display: inline-flex;
  align-items: center;
  gap: 6px;
`;
