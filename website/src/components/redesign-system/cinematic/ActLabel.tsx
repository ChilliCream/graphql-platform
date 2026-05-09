"use client";

import React from "react";
import styled, { css } from "styled-components";

// Numbered chapter-marker eyebrow lifted from the homepage's `.cc-act-label`
// (see `landing/desktop/DesktopLandingRoot.tsx` lines 59-82). A two-digit
// numeral sits inside a hairline 1px box, followed by the act name in
// uppercase monospace with 0.18em tracking.
//
// Drop directly inside a `<Band>` to chapter the section. The label is
// absolutely positioned at top: 36px, left: var(--cc-pad-x) so it sits in
// the band's gutter ABOVE the inner content. Note the band must allow the
// label to render in the section's normal flow (it sits inside the gutter,
// not above the section), so a typical `<Band>` works without changes; if
// you wrap the label in a band variant that clips overflow tighter than
// the homepage's `.cc-act` (which uses `overflow: hidden` but still lets
// the label render because it lives inside the section bounds), set
// `overflow: visible` on that variant.

export type ActLabelAlign = "left" | "center";

export interface ActLabelProps {
  /** Two-digit chapter number, e.g. "01". */
  n: string;
  /** Chapter name, will be uppercased and letter-spaced by the component. */
  name: string;
  /** Horizontal placement within the band gutter. Defaults to `left`. */
  align?: ActLabelAlign;
  className?: string;
}

interface OuterProps {
  $align: ActLabelAlign;
}

const Outer = styled.div<OuterProps>`
  position: absolute;
  top: 36px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  color: var(--cc-ink-dim);
  text-transform: uppercase;
  z-index: 4;
  display: inline-flex;
  align-items: center;
  gap: 10px;

  ${({ $align }) =>
    $align === "center"
      ? css`
          left: 50%;
          transform: translateX(-50%);
        `
      : css`
          left: var(--cc-pad-x, 28px);
        `}
`;

const Numeral = styled.span`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 3px 7px;
  border: 1px solid var(--cc-ink-faint);
  border-radius: 4px;
  color: var(--cc-ink);
  line-height: 1;
`;

/**
 * Chapter-marker eyebrow with a hairline numeral box and an uppercase
 * monospace name. Place inside a `<Band>` to chapter the section.
 */
export const ActLabel: React.FC<ActLabelProps> = ({
  n,
  name,
  align = "left",
  className,
}) => {
  return (
    <Outer $align={align} className={className}>
      <Numeral>{n}</Numeral>
      <span>{name}</span>
    </Outer>
  );
};
