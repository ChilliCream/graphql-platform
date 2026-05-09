"use client";

import Link from "next/link";
import React from "react";
import styled from "styled-components";

// End-of-page set-piece distilled from the homepage's `.cc-act-final-cta` +
// `.cc-final-cta-inner-d` (see `landing/desktop/DesktopLandingRoot.tsx` lines
// 747-768). Display-tier headline that becomes the illustration, dim sub-copy,
// and a centered two-button CTA row. One per page, last band before the
// footer.

export interface MegaCtaButton {
  /** Visible button text. */
  label: string;
  /** Destination URL. */
  href: string;
}

export interface MegaCtaProps {
  /** Display-tier headline rendered at clamp(48px, 6.4vw, 96px). */
  headline: string;
  /** Optional subhead body copy, dim ink, max 52ch. */
  body?: string;
  /** Primary call to action. Filled cream button. */
  primaryCta: MegaCtaButton;
  /** Optional secondary call to action. Ghost outline button. */
  secondaryCta?: MegaCtaButton;
  className?: string;
}

const Outer = styled.div`
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding-top: clamp(96px, 14vw, 160px);
  padding-bottom: clamp(96px, 14vw, 160px);
`;

const Inner = styled.div`
  max-width: 880px;
  margin: 0 auto;
`;

const Headline = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(48px, 6.4vw, 96px);
  font-weight: 500;
  letter-spacing: -0.04em;
  line-height: 0.96;
  color: var(--cc-ink);
  margin: 0 0 24px;
  text-wrap: balance;
`;

const Body = styled.p`
  font-size: clamp(16px, 1.2vw, 19px);
  line-height: 1.5;
  color: var(--cc-ink-dim);
  max-width: 52ch;
  margin: 0 auto 32px;
  text-wrap: pretty;
`;

const Row = styled.div`
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 14px;
`;

const baseButton = `
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  padding: 16px 26px;
  border-radius: 999px;
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 15px;
  font-weight: 500;
  text-decoration: none;
  cursor: pointer;
  transition: transform 0.12s ease, background 0.12s ease,
    border-color 0.12s ease;
`;

const PrimaryButton = styled(Link)`
  ${baseButton}
  background: var(--cc-ink);
  color: #0c1322;
  border: none;

  &:hover,
  &:focus-visible {
    transform: translateY(-1px);
  }
`;

const SecondaryButton = styled(Link)`
  ${baseButton}
  background: transparent;
  color: var(--cc-ink);
  border: 1px solid var(--cc-ink-faint);

  &:hover,
  &:focus-visible {
    border-color: var(--cc-ink);
  }
`;

/**
 * Centered display-tier set-piece with optional body copy and a two-button
 * CTA row. Use as the last band before the footer; one per page.
 */
export const MegaCta: React.FC<MegaCtaProps> = ({
  headline,
  body,
  primaryCta,
  secondaryCta,
  className,
}) => {
  return (
    <Outer className={className}>
      <Inner>
        <Headline>{headline}</Headline>
        {body ? <Body>{body}</Body> : null}
        <Row>
          <PrimaryButton href={primaryCta.href}>
            {primaryCta.label}
          </PrimaryButton>
          {secondaryCta ? (
            <SecondaryButton href={secondaryCta.href}>
              {secondaryCta.label}
            </SecondaryButton>
          ) : null}
        </Row>
      </Inner>
    </Outer>
  );
};
