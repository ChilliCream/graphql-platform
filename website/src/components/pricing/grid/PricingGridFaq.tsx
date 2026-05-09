"use client";

import React from "react";
import styled from "styled-components";

import {
  GridCard,
  GridSection,
  GridSplit,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import { FAQ } from "@/data/pricing/faq";

const Chevron: React.FC = () => (
  <svg viewBox="0 0 24 24" width="20" height="20" aria-hidden>
    <path
      d="M6 9 L12 15 L18 9"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

// Default-open the most-asked billing question (index 3 in the canonical
// FAQ ordering: "How is a request counted?"). Same starting state as the
// Default variant so readers don't lose context across variant switches.
const DEFAULT_OPEN_INDEX = 3;

// Archetype K. Asymmetric split: the section heading occupies the left rail,
// the accordion runs in the right column. Each row is separated by a 1px
// hairline; the chevron flips on open. The whole accordion lives inside a
// single GridCard with internal dividers.
export const PricingGridFaq: React.FC = () => {
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "FAQPage",
    mainEntity: FAQ.map(({ q, a }) => ({
      "@type": "Question",
      name: q,
      acceptedAnswer: {
        "@type": "Answer",
        text: a,
      },
    })),
  };

  return (
    <GridSection variant="default" hairlineBottom>
      <GridSplit ratio="33-66">
        <LeftRail>
          <Eyebrow>FAQ</Eyebrow>
          <Title>Frequently asked questions.</Title>
          <Lede>
            Honest answers about billing, limits, and what triggers an upgrade.
          </Lede>
        </LeftRail>

        <GridCard noPadding>
          <List>
            {FAQ.map((item, index) => (
              <Item key={item.q} open={index === DEFAULT_OPEN_INDEX}>
                <Summary>
                  <Num>{String(index + 1).padStart(2, "0")}</Num>
                  <Question>{item.q}</Question>
                  <ChevronWrap>
                    <Chevron />
                  </ChevronWrap>
                </Summary>
                <Answer>{item.a}</Answer>
              </Item>
            ))}
          </List>
        </GridCard>
      </GridSplit>

      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
    </GridSection>
  );
};

const LeftRail = styled.div`
  padding: clamp(28px, 3vw, 40px);
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
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
  text-wrap: balance;
`;

const Lede = styled.p`
  font-size: 15px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  max-width: 36ch;
  margin: 0;
  text-wrap: pretty;
`;

const List = styled.div`
  display: flex;
  flex-direction: column;
`;

const Item = styled.details`
  border-bottom: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});

  &:last-child {
    border-bottom: 0;
  }

  &[open] svg {
    transform: rotate(180deg);
    color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  }
`;

const Summary = styled.summary`
  list-style: none;
  cursor: pointer;
  display: flex;
  align-items: flex-start;
  gap: 16px;
  padding: 22px 24px;
  font-size: 16px;
  font-weight: 500;
  color: ${GRID_TOKENS.inkPrimary};
  transition: color 0.15s ease;

  &::-webkit-details-marker {
    display: none;
  }

  &:hover {
    color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  }
`;

const Num = styled.span`
  flex-shrink: 0;
  width: 28px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  letter-spacing: 0.14em;
  color: ${GRID_TOKENS.inkFaint};
  padding-top: 3px;
`;

const Question = styled.span`
  flex: 1;
  text-align: left;
  line-height: 1.4;
`;

const ChevronWrap = styled.span`
  flex-shrink: 0;
  display: inline-flex;
  color: ${GRID_TOKENS.inkMuted};

  svg {
    transition: transform 0.2s ease, color 0.15s ease;
  }
`;

const Answer = styled.p`
  margin: 0;
  padding: 0 24px 24px 68px;
  color: ${GRID_TOKENS.inkBody};
  font-size: 15px;
  line-height: 1.65;
  text-wrap: pretty;
  max-width: 70ch;
`;
