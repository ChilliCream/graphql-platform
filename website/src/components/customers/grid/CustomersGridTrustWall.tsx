"use client";

import React, { CSSProperties, FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import { findIndustry } from "@/data/customers/industries";
import { TRUST_WALL_ALL } from "@/data/customers/stories";

// 04 Trust wall (archetype H, 6-up): typographic lockups for the long
// tail of customers we can't always logo. Each tile is a `GridCard` cell
// inside a 6-up `GridRow`, with the industry chip as a small mono caption
// taking the industry color, and the descriptor + fact as the typographic
// content. NO logo monograms, no vibrant tinted tiles, just the Default
// typographic treatment per the spec.
const TILES_PER_ROW = 6;

export const CustomersGridTrustWall: FC = () => {
  // Render a stable subset so the wall is always 4 rows of 6 = 24 cells,
  // densest possible at 6-up without breaking the typographic rhythm.
  const tiles = TRUST_WALL_ALL.slice(0, 24);

  return (
    <GridSection hairlineBottom>
      <Heading>
        <Eyebrow>Trusted by</Eyebrow>
        <H2>Names where allowed. Sectors where not.</H2>
        <Sub>
          We can&apos;t always logo a customer. We can always tell you which
          sector they&apos;re in, what scale they run at, and what they
          replaced.
        </Sub>
      </Heading>

      {Array.from({ length: Math.ceil(tiles.length / TILES_PER_ROW) }).map(
        (_, rowIndex) => {
          const slice = tiles.slice(
            rowIndex * TILES_PER_ROW,
            (rowIndex + 1) * TILES_PER_ROW
          );
          return (
            <GridRow key={rowIndex} cols={6}>
              {slice.map((tile) => {
                const industry = findIndustry(tile.industry);
                const tileStyle: CSSProperties = {
                  ["--cc-trust-accent" as string]: industry.accentVar,
                };
                return (
                  <GridCard key={tile.key}>
                    <Tile style={tileStyle}>
                      <Chip>{industry.short}</Chip>
                      <Descriptor $named={tile.named}>
                        {tile.caption}
                      </Descriptor>
                      {tile.fact ? <Fact>{tile.fact}</Fact> : null}
                    </Tile>
                  </GridCard>
                );
              })}
            </GridRow>
          );
        }
      )}
    </GridSection>
  );
};

const Heading = styled.div`
  margin: 0 0 clamp(36px, 4vw, 56px);
  display: flex;
  flex-direction: column;
  gap: 14px;
  max-width: 760px;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: ${GRID_TOKENS.eyebrowSize};
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const H2 = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(28px, 3.4vw, 44px);
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.1;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  max-width: 22ch;
  text-wrap: balance;
`;

const Sub = styled.p`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(15px, 1.1vw, 17px);
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  max-width: 60ch;
  text-wrap: pretty;
`;

const Tile = styled.div`
  display: flex;
  flex-direction: column;
  gap: 10px;
  align-items: flex-start;
  min-height: 132px;
`;

const Chip = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-trust-accent, ${GRID_TOKENS.inkMuted});
`;

interface DescriptorProps {
  $named: boolean;
}

const Descriptor = styled.span<DescriptorProps>`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: ${({ $named }) =>
    $named ? "clamp(15px, 1.15vw, 18px)" : "clamp(13px, 1vw, 15px)"};
  font-weight: ${({ $named }) => ($named ? 600 : 500)};
  letter-spacing: -0.01em;
  line-height: 1.25;
  color: ${GRID_TOKENS.inkPrimary};
  text-wrap: pretty;
`;

const Fact = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
  line-height: 1.4;
  margin-top: auto;
`;
