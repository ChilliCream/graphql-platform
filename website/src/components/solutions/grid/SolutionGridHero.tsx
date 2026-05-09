"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridButton,
  GridCard,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import type {
  HeroMotif as HeroMotifKind,
  SolutionHero as SolutionHeroData,
} from "@/data/solutions/types";

import { HeroMotif } from "../HeroMotif";

interface SolutionGridHeroProps {
  readonly hero: SolutionHeroData;
  readonly motif?: HeroMotifKind;
  readonly slug: string;
}

// Archetype A/B (text + optional motif). The headline + sub + 2-button row
// sit on the page surface; if a motif is supplied it lives inside a
// noPadding GridCard on the right so the hero reads as 7/12 + 5/12 with a
// shared center hairline. Closes with a 1px bottom hairline that the next
// section inherits as its top edge.
export const SolutionGridHero: FC<SolutionGridHeroProps> = ({
  hero,
  motif,
  slug,
}) => {
  const { eyebrow, headline, sub, primaryCta, secondaryCta } = hero;

  return (
    <GridSection hairlineBottom>
      <Layout $hasMotif={Boolean(motif)}>
        <Copy>
          <span className="cc-grid-eyebrow">{eyebrow}</span>
          <h1 className="cc-grid-h1">{headline}</h1>
          <p className="cc-grid-lede">{sub}</p>
          <CtaRow>
            <GridButton variant="primary" href={primaryCta.href}>
              {primaryCta.label}
            </GridButton>
            <GridButton variant="secondary" href={secondaryCta.href}>
              {secondaryCta.label}
            </GridButton>
          </CtaRow>
        </Copy>
        {motif ? (
          <MotifSlot>
            <GridCard noPadding>
              <MotifInner aria-hidden>
                <HeroMotif kind={motif} slug={slug} />
              </MotifInner>
            </GridCard>
          </MotifSlot>
        ) : null}
      </Layout>
    </GridSection>
  );
};

interface LayoutProps {
  $hasMotif: boolean;
}

const Layout = styled.div<LayoutProps>`
  display: grid;
  grid-template-columns: ${({ $hasMotif }) =>
    $hasMotif ? "minmax(0, 1.4fr) minmax(0, 1fr)" : "1fr"};
  gap: clamp(32px, 5vw, 64px);
  align-items: center;
  min-height: clamp(360px, 48vh, 480px);

  @media (max-width: 960px) {
    grid-template-columns: 1fr;
  }
`;

const Copy = styled.div`
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-width: 720px;
`;

const CtaRow = styled.div`
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  margin-top: 28px;
`;

const MotifSlot = styled.div`
  width: 100%;
  display: flex;
  align-items: stretch;
`;

const MotifInner = styled.div`
  width: 100%;
  aspect-ratio: 4 / 3;
  max-height: 400px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: ${GRID_TOKENS.inkPrimary};
  padding: 24px;

  > svg {
    width: 100%;
    height: 100%;
    display: block;
  }
`;
