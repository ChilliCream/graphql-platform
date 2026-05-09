"use client";

import Link from "next/link";
import React, { FC } from "react";
import styled from "styled-components";

import {
  GridRow,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import { STORIES, type Story } from "@/data/customers/stories";

// 05 All stories (archetype D, 3-up smaller). Same card pattern as the
// featured rail but rendered tighter: a denser 3-up grid where every
// shipped story gets a tile. Filters belong to the Default and Cinematic
// variants; Grid renders the long tail in canonical 3-up shared-border
// rows because the discipline is "the grid does the work."
const ROW_SIZE = 3;

export const CustomersGridAllStories: FC = () => {
  return (
    <GridSection hairlineBottom>
      <Heading>
        <Eyebrow>All stories</Eyebrow>
        <H2>The long tail.</H2>
        <Sub>
          Every shipped story, oldest to newest. Click through for the full case
          body, the at-a-glance sidebar, and the metric in context.
        </Sub>
      </Heading>

      {Array.from({ length: Math.ceil(STORIES.length / ROW_SIZE) }).map(
        (_, rowIndex) => {
          const slice = STORIES.slice(
            rowIndex * ROW_SIZE,
            (rowIndex + 1) * ROW_SIZE
          );
          // Pad the final row with empty bordered cells so the grid frame
          // closes. Without this, an incomplete row reads as a torn edge.
          const padding = ROW_SIZE - slice.length;
          return (
            <GridRow key={rowIndex} cols={3}>
              {slice.map((story) => (
                <StoryCell key={story.slug} story={story} />
              ))}
              {Array.from({ length: padding }).map((__, padIdx) => (
                <Filler key={`pad-${rowIndex}-${padIdx}`} aria-hidden />
              ))}
            </GridRow>
          );
        }
      )}
    </GridSection>
  );
};

interface StoryCellProps {
  readonly story: Story;
}

const StoryCell: FC<StoryCellProps> = ({ story }) => {
  const lockup = story.named
    ? story.displayName
    : story.descriptor ?? story.displayName.toUpperCase();

  return (
    <CellLink href={`/customers/${story.slug}`}>
      <CellInner>
        <CellEyebrow>{story.eyebrow}</CellEyebrow>
        <CellMetric>{story.cardMetric}</CellMetric>
        <CellFoot>
          <Lockup $named={story.named}>{lockup}</Lockup>
          <Arrow aria-hidden>→</Arrow>
        </CellFoot>
      </CellInner>
    </CellLink>
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

const Filler = styled.div`
  background: ${GRID_TOKENS.bgBase};
  min-height: 192px;
`;

const CellInner = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: clamp(20px, 2vw, 28px);
  min-height: 192px;
`;

const CellEyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const CellMetric = styled.p`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(17px, 1.7vw, 21px);
  font-weight: 600;
  letter-spacing: -0.015em;
  line-height: 1.2;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  flex: 1;
  text-wrap: balance;
`;

const CellFoot = styled.div`
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 12px;
  padding-top: 14px;
  border-top: 1px solid ${GRID_TOKENS.hairline};
`;

interface LockupProps {
  $named: boolean;
}

const Lockup = styled.span<LockupProps>`
  font-family: ${({ $named }) =>
    $named
      ? "var(--cc-font-sans), sans-serif"
      : "var(--cc-font-mono), monospace"};
  font-size: ${({ $named }) => ($named ? "14px" : "10px")};
  font-weight: ${({ $named }) => ($named ? 600 : 500)};
  letter-spacing: ${({ $named }) => ($named ? "-0.01em" : "0.16em")};
  text-transform: ${({ $named }) => ($named ? "none" : "uppercase")};
  color: ${GRID_TOKENS.inkPrimary};
  line-height: 1.2;
  max-width: 70%;
  text-wrap: pretty;
`;

const Arrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 14px;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  transition: transform 0.18s ease;

  ${CellLink}:hover & {
    transform: translateX(2px);
  }
`;
