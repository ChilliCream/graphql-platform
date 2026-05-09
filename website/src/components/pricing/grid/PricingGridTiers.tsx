"use client";

import React from "react";
import styled from "styled-components";

import {
  GridButton,
  GridCard,
  GridRow,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import { TIERS } from "@/data/pricing/tiers";

// Archetype D-as-tier. Three flush tier cells share a 1px hairline frame.
// Card chrome stays monochrome; the bullet chevrons take the per-page accent
// color so the page identity surfaces inside the cell.
export const PricingGridTiers: React.FC = () => {
  return (
    <GridSection variant="default" hairlineBottom>
      <SectionHeading>
        <Eyebrow>Plans</Eyebrow>
        <Title>Brew it your way.</Title>
        <Lede>
          Same engine, same APIs, same DX. Pick the operational shape that fits
          your team. Move between them without re-architecting.
        </Lede>
      </SectionHeading>

      <GridRow cols={3}>
        {TIERS.map((tier) => (
          <GridCard key={tier.key} as="article">
            <TierBody>
              {tier.badge ? <Badge>{tier.badge}</Badge> : <BadgeSpacer />}
              <Brewer>{tier.brewer}</Brewer>
              <TierName>{tier.name}</TierName>
              <Tagline>{tier.tagline}</Tagline>

              <PriceBlock>
                <PriceAmount>{tier.price}</PriceAmount>
                <PriceNote>{tier.priceNote}</PriceNote>
              </PriceBlock>

              <Bullets>
                {tier.bullets.map((bullet) => (
                  <li key={bullet}>
                    <Chevron aria-hidden="true">→</Chevron>
                    <span>{bullet}</span>
                  </li>
                ))}
              </Bullets>

              <CtaSlot>
                <GridButton
                  variant={tier.featured ? "primary" : "secondary"}
                  href={tier.ctaHref}
                >
                  {tier.cta}
                </GridButton>
              </CtaSlot>
            </TierBody>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};

const SectionHeading = styled.div`
  text-align: center;
  max-width: 760px;
  margin: 0 auto 56px;
`;

const Eyebrow = styled.div`
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
  margin-bottom: 16px;
`;

const Title = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: ${GRID_TOKENS.h2Size};
  font-weight: 600;
  line-height: 1.05;
  letter-spacing: -0.03em;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0 0 14px;
`;

const Lede = styled.p`
  font-size: 16px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  max-width: 56ch;
  margin: 0 auto;
  text-wrap: pretty;
`;

const TierBody = styled.div`
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 520px;
`;

const Badge = styled.div`
  align-self: flex-start;
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  font-weight: 500;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.bgBase};
  background: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  padding: 4px 8px;
  margin-bottom: 20px;
`;

const BadgeSpacer = styled.div`
  height: 22px;
  margin-bottom: 20px;
`;

const Brewer = styled.div`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkFaint};
  margin-bottom: 8px;
`;

const TierName = styled.h3`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 24px;
  font-weight: 600;
  letter-spacing: -0.02em;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0 0 8px;
`;

const Tagline = styled.p`
  font-size: 14px;
  line-height: 1.5;
  color: ${GRID_TOKENS.inkBody};
  margin: 0 0 28px;
`;

const PriceBlock = styled.div`
  padding: 24px 0;
  border-top: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  margin-bottom: 24px;
`;

const PriceAmount = styled.div`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(36px, 4vw, 48px);
  font-weight: 600;
  line-height: 1;
  letter-spacing: -0.03em;
  color: ${GRID_TOKENS.inkPrimary};
  font-feature-settings: "tnum" 1;
`;

const PriceNote = styled.div`
  margin-top: 6px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const Bullets = styled.ul`
  list-style: none;
  padding: 0;
  margin: 0 0 28px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  flex: 1;

  li {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    font-size: 14px;
    line-height: 1.5;
    color: ${GRID_TOKENS.inkBody};
  }
`;

const Chevron = styled.span`
  flex-shrink: 0;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  font-family: var(--cc-font-mono), monospace;
  font-weight: 500;
  line-height: 1.5;
`;

const CtaSlot = styled.div`
  margin-top: auto;

  a {
    width: 100%;
  }
`;
