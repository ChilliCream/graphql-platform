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

// 03 Featured rail (archetype D, 3-up x 2-rows): six case-study tiles.
// Each cell follows the canonical 3-up benefit layout (eyebrow + headline
// + metric + monogram/wordmark + arrow CTA) but rendered into the
// hairline-bordered grid frame. The whole rail is a single bordered grid
// where adjacent cells share borders; no inner gaps, no rounded corners,
// no drop shadow.
export const CustomersGridFeaturedRail: FC = () => {
  const featured = STORIES.filter((s) => s.featured).slice(0, 6);

  return (
    <GridSection hairlineBottom id="stories">
      <Heading>
        <Eyebrow>Featured stories</Eyebrow>
        <H2>The win first. The brand second.</H2>
        <Sub>
          Six platform teams. Six different stacks. Six metrics that paid for
          the rollout in the first quarter. Some named, some not, every one real
          and verified.
        </Sub>
      </Heading>
      <GridRow cols={3}>
        {featured.map((story) => (
          <FeaturedCell key={story.slug} story={story} />
        ))}
      </GridRow>
    </GridSection>
  );
};

interface FeaturedCellProps {
  readonly story: Story;
}

const FeaturedCell: FC<FeaturedCellProps> = ({ story }) => {
  const lockup = story.named
    ? story.displayName
    : story.descriptor ?? story.displayName.toUpperCase();

  return (
    <CellLink href={`/customers/${story.slug}`}>
      <CellInner>
        <CellEyebrow>{story.eyebrow}</CellEyebrow>
        <CellMetric>{story.cardMetric}</CellMetric>
        <CellBody>{story.cardContext}</CellBody>
        <CellFoot>
          <Lockup $named={story.named}>{lockup}</Lockup>
          <ReadStory>
            Read story <Arrow aria-hidden>→</Arrow>
          </ReadStory>
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

const CellInner = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: clamp(24px, 2.4vw, 32px);
  min-height: 320px;
`;

const CellEyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const CellMetric = styled.p`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(20px, 2vw, 26px);
  font-weight: 600;
  letter-spacing: -0.02em;
  line-height: 1.15;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  text-wrap: balance;
`;

const CellBody = styled.p`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  flex: 1;
  text-wrap: pretty;
`;

const CellFoot = styled.div`
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  padding-top: 16px;
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
  font-size: ${({ $named }) => ($named ? "16px" : "11px")};
  font-weight: ${({ $named }) => ($named ? 600 : 500)};
  letter-spacing: ${({ $named }) => ($named ? "-0.01em" : "0.16em")};
  text-transform: ${({ $named }) => ($named ? "none" : "uppercase")};
  color: ${GRID_TOKENS.inkPrimary};
  line-height: 1.2;
  max-width: 60%;
  text-wrap: pretty;
`;

const ReadStory = styled.span`
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkPrimary};
  flex-shrink: 0;
`;

const Arrow = styled.span`
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  transition: transform 0.18s ease;

  ${CellLink}:hover & {
    transform: translateX(2px);
  }
`;
