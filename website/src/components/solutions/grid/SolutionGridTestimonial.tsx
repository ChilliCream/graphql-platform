"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridSection, GRID_TOKENS } from "@/components/redesign-system/grid";
import type { Testimonial } from "@/data/solutions/types";

interface SolutionGridTestimonialProps {
  readonly testimonials: readonly Testimonial[];
}

// Archetype I (testimonial). Per Grid spec: a single quote occupies the
// full text-column width, with no card chrome around the quote (just
// hairline above and below from the section's own borders). Multiple
// testimonials stack vertically separated by a single 1px hairline rule.
export const SolutionGridTestimonial: FC<SolutionGridTestimonialProps> = ({
  testimonials,
}) => {
  if (testimonials.length === 0) {
    return null;
  }

  return (
    <GridSection hairlineBottom>
      <Wrap>
        {testimonials.map((t, i) => (
          <Quote key={`${t.company}-${i}`}>
            <Body>&ldquo;{t.quote}&rdquo;</Body>
            <Attribution>
              <Name>{t.title}</Name>
              <Sep>·</Sep>
              <Company>{t.company}</Company>
            </Attribution>
          </Quote>
        ))}
      </Wrap>
    </GridSection>
  );
};

const Wrap = styled.div`
  max-width: 980px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
`;

const Quote = styled.figure`
  margin: 0;
  padding: clamp(48px, 7vw, 88px) 0;
  display: flex;
  flex-direction: column;
  gap: 28px;

  & + & {
    border-top: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  }
`;

const Body = styled.blockquote`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(22px, 2.4vw, 32px);
  font-weight: 500;
  line-height: 1.35;
  letter-spacing: -0.015em;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
  text-wrap: pretty;
`;

const Attribution = styled.figcaption`
  display: inline-flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const Name = styled.span`
  color: ${GRID_TOKENS.inkPrimary};
`;

const Sep = styled.span`
  color: ${GRID_TOKENS.inkFaint};
`;

const Company = styled.span`
  color: ${GRID_TOKENS.inkBody};
`;
