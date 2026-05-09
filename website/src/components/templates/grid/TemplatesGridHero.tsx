"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridButton, GridSection } from "@/components/redesign-system/grid";

// Section 01: minimal text-only hero. Per the Grid spec for /templates the
// hero is intentionally lean: a centered eyebrow + h1 + sub + 3-button row.
// No featured-template-as-hero (that pattern belongs to the Default variant).
// The strict square frame is closed by the section's bottom hairline so the
// gallery below shares the line.
export const TemplatesGridHero: FC = () => {
  return (
    <GridSection hairlineTop hairlineBottom>
      <Inner>
        <Eyebrow>Templates</Eyebrow>
        <Heading>Start with a template.</Heading>
        <Sub>
          Production-ready GraphQL services, federations, and clients. Clone,
          customize, ship.
        </Sub>
        <Actions>
          <GridButton variant="primary" href="#templates-gallery">
            Browse gallery
          </GridButton>
          <GridButton
            variant="secondary"
            href="https://github.com/ChilliCream/templates"
          >
            View on GitHub
          </GridButton>
          <GridButton variant="ghost" href="https://chillicream.com/docs">
            Read the docs
          </GridButton>
        </Actions>
      </Inner>
    </GridSection>
  );
};

const Inner = styled.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 22px;
  max-width: 760px;
  margin: 0 auto;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  font-weight: 500;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, currentColor);
`;

const Heading = styled.h1`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(40px, 5.5vw, 72px);
  font-weight: 600;
  line-height: 1.05;
  letter-spacing: -0.02em;
  color: inherit;
  margin: 0;
  text-wrap: balance;
`;

const Sub = styled.p`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(15px, 1.2vw, 18px);
  line-height: 1.55;
  color: rgba(245, 241, 234, 0.72);
  margin: 0;
  max-width: 56ch;
  text-wrap: pretty;
`;

const Actions = styled.div`
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 12px;
  margin-top: 12px;
`;
