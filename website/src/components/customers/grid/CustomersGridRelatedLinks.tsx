"use client";

import Link from "next/link";
import React, { FC } from "react";
import styled from "styled-components";

import {
  GridRow,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";

interface RelatedLink {
  readonly key: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly body: string;
  readonly href: string;
  readonly cta: string;
}

const RELATED: readonly RelatedLink[] = [
  {
    key: "pricing",
    eyebrow: "Pricing",
    title: "See what each tier ships with",
    body: "Free OSS through enterprise. Hard limits, budget alerts, and the same engine underneath.",
    href: "/pricing",
    cta: "Pricing",
  },
  {
    key: "enterprise",
    eyebrow: "Enterprise",
    title: "Nitro for enterprise platform teams",
    body: "Self-hosted, air-gapped, agent-ready, supported by the engineers who built it.",
    href: "/enterprise",
    cta: "Enterprise",
  },
  {
    key: "support",
    eyebrow: "Support",
    title: "Support plans + dedicated SAs",
    body: "Custom SLAs, 24x7 oncall, federation governance, and procurement-ready compliance.",
    href: "/pricing#support",
    cta: "Support plans",
  },
];

// 07 Related links (archetype D, 3-up). Three navigation tiles back into
// the rest of the site. Each cell is its own anchor inside the bordered
// `GridRow`, with eyebrow + title + body + a small `→` CTA. Closes the
// page with a hairline foot that anchors the surface above the global
// footer.
export const CustomersGridRelatedLinks: FC = () => {
  return (
    <GridSection hairlineBottom>
      <Heading>
        <Eyebrow>Related</Eyebrow>
        <H3>Three more places to go.</H3>
      </Heading>
      <GridRow cols={3}>
        {RELATED.map((link) => (
          <CellLink key={link.key} href={link.href}>
            <CellInner>
              <CellEyebrow>{link.eyebrow}</CellEyebrow>
              <CellTitle>{link.title}</CellTitle>
              <CellBody>{link.body}</CellBody>
              <CellCta>
                {link.cta} <Arrow aria-hidden>→</Arrow>
              </CellCta>
            </CellInner>
          </CellLink>
        ))}
      </GridRow>
    </GridSection>
  );
};

const Heading = styled.div`
  margin: 0 0 clamp(28px, 3vw, 40px);
  display: flex;
  flex-direction: column;
  gap: 10px;
  max-width: 760px;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: ${GRID_TOKENS.eyebrowSize};
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const H3 = styled.h3`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(22px, 2.4vw, 30px);
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.2;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
`;

const CellLink = styled(Link)`
  display: block;
  background: ${GRID_TOKENS.bgCard};
  color: ${GRID_TOKENS.inkPrimary};
  text-decoration: none;
  transition: background 0.15s ease;

  &:hover,
  &:focus-visible {
    background: ${GRID_TOKENS.bgHover};
  }
`;

const CellInner = styled.div`
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: clamp(20px, 2vw, 28px);
  min-height: 200px;
`;

const CellEyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const CellTitle = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(16px, 1.5vw, 19px);
  font-weight: 600;
  letter-spacing: -0.015em;
  line-height: 1.25;
  color: ${GRID_TOKENS.inkPrimary};
  text-wrap: pretty;
`;

const CellBody = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  flex: 1;
  text-wrap: pretty;
`;

const CellCta = styled.span`
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkPrimary};
  margin-top: auto;
`;

const Arrow = styled.span`
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  transition: transform 0.18s ease;

  ${CellLink}:hover & {
    transform: translateX(2px);
  }
`;
