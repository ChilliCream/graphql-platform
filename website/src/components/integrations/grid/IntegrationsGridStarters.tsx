"use client";

import Link from "next/link";
import React, { FC } from "react";
import styled from "styled-components";

import {
  GRID_TOKENS,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";

// Three starter projects pairing ChilliCream with a partner. Cards link
// straight into /templates with a topology pre-filter, so the integration
// page hands off to a working copy-paste flow rather than a dead "look at
// this" surface. Same data the Default and Cinematic variants render.
const STARTERS = [
  {
    slug: "fusion-with-nitro-observability",
    title: "Fusion + Nitro Observability",
    stack: "Hot Chocolate / Fusion / Nitro / OpenTelemetry",
    blurb:
      "Three-service Fusion mesh with Nitro tracing wired in. The Operator's Window starter, ready to deploy.",
  },
  {
    slug: "agent-ready-api",
    title: "Agent-Ready API",
    stack: "Hot Chocolate / Nitro / MCP",
    blurb:
      "A solo Hot Chocolate service that talks GraphQL to humans and MCP to agents. Same auth, same schema.",
  },
  {
    slug: "multi-tenant-saas-starter",
    title: "Multi-tenant SaaS Starter",
    stack: "Hot Chocolate / Nitro / Auth0 / Postgres",
    blurb:
      "Per-tenant isolation, RBAC, audit log, and a Next.js admin console wired to the same GraphQL endpoint.",
  },
] as const;

export const IntegrationsGridStarters: FC = () => {
  return (
    <GridSection variant="default" hairlineTop>
      <Head>
        <Eyebrow>Starter templates</Eyebrow>
        <Title>See the integrations working together.</Title>
        <Tagline>
          Three opinionated starters pairing ChilliCream with the integrations
          above. Clone, run, customize.
        </Tagline>
      </Head>
      <GridRow cols={3}>
        {STARTERS.map((s) => (
          <Starter key={s.slug} href={`/templates/${s.slug}`}>
            <Stack>{s.stack}</Stack>
            <StarterTitle>{s.title}</StarterTitle>
            <Blurb>{s.blurb}</Blurb>
            <Cta>
              Open template <span aria-hidden>&rarr;</span>
            </Cta>
          </Starter>
        ))}
      </GridRow>
    </GridSection>
  );
};

const Head = styled.div`
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 28px;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const Title = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(24px, 2.6vw, 32px);
  font-weight: 600;
  letter-spacing: -0.025em;
  line-height: 1.05;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
`;

const Tagline = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  max-width: 60ch;
`;

const Starter = styled(Link)`
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 28px;
  background: var(--cc-grid-card-bg, ${GRID_TOKENS.bgCard});
  color: ${GRID_TOKENS.inkPrimary};
  text-decoration: none;
  min-height: 240px;
  transition: background 0.12s ease;

  &:hover {
    background: var(--cc-grid-card-hover, ${GRID_TOKENS.bgHover});
  }
`;

const Stack = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const StarterTitle = styled.h3`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 20px;
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.2;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
`;

const Blurb = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  flex: 1;
  text-wrap: pretty;
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
