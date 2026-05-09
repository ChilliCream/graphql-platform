"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridSection } from "@/components/redesign-system/grid";

// Section 10 (Grid): final CTA. Centered headline + a 3-button row. Lives
// in a regular GridSection with a top hairline closing off the section
// above. The three buttons follow the spec request (3-button row): primary
// install, secondary docs, ghost demo, all square-cornered.

const Inner = styled.div`
  max-width: 720px;
  margin: 0 auto;
  text-align: center;
  padding: 32px 0 64px;

  .eyebrow {
    color: var(--cc-accent, var(--cc-amber));
    margin-bottom: 12px;
  }
  h2 {
    font-family: var(--cc-font-sans), sans-serif;
    font-size: clamp(36px, 5vw, 64px);
    font-weight: 600;
    letter-spacing: -0.02em;
    line-height: 1.05;
    margin: 0 0 32px;
  }
  h2 .accent {
    color: var(--cc-accent, var(--cc-amber));
  }
`;

const Row = styled.div`
  display: flex;
  gap: 12px;
  justify-content: center;
  flex-wrap: wrap;

  a.cc-btn {
    border-radius: 0;
    padding: 12px 22px;
  }
`;

export const AgentsGridFinalCta: FC = () => {
  return (
    <GridSection variant="default" hairlineTop>
      <Inner>
        <div className="eyebrow">Ready?</div>
        <h2>
          Stop being the <span className="accent">human glue.</span>
        </h2>
        <Row>
          <a href="/docs/nitro" className="cc-btn cc-btn-primary">
            Install Nitro CLI
          </a>
          <a href="/docs/nitro/agents" className="cc-btn cc-btn-ghost">
            Read the docs
          </a>
          <a
            href="mailto:contact@chillicream.com?subject=Nitro%20agents%20demo"
            className="cc-btn cc-btn-ghost"
          >
            Book a demo
          </a>
        </Row>
      </Inner>
    </GridSection>
  );
};
