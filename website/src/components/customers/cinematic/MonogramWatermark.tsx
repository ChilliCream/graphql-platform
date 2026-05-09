"use client";

import React from "react";
import styled from "styled-components";

// Quiet typographic watermark for the cinematic /customers variant.
// Renders a single massive outlined "CC" monogram (the ChilliCream
// initials) anchored to the right edge of the page so the second `C`
// bleeds off the viewport. The mark sits at 5% opacity, like a
// debossed letterhead stamp, and a single hairline rule runs across
// the very top of the page as a "letterhead rule".
//
// The watermark sits at `position: absolute; inset: 0; z-index: 0;`
// behind the page content. It is purely decorative and aria-hidden.

export interface MonogramWatermarkProps {
  className?: string;
}

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
`;

// Hairline letterhead rule across the very top of the page.
const Hairline = styled.div`
  position: absolute;
  top: 200px;
  left: 0;
  right: 0;
  height: 1px;
  background: rgba(245, 241, 234, 0.06);
`;

// The monogram itself. Anchored top-right, bleeding off the right
// edge so the second `C` is clipped by the viewport. Sized via clamp
// so it adapts to small and large viewports without any media query.
const MonogramSvg = styled.svg`
  position: absolute;
  top: 12%;
  right: -8%;
  height: clamp(480px, 60vh, 800px);
  width: auto;
  display: block;
`;

/**
 * Decorative typographic watermark for the cinematic /customers
 * variant. Renders a single oversized outlined "CC" monogram bleeding
 * off the right edge plus a thin letterhead rule at the top of the
 * page. Hidden from assistive tech and never receives pointer events.
 */
export const MonogramWatermark: React.FC<MonogramWatermarkProps> = ({
  className,
}) => {
  return (
    <Outer className={className} aria-hidden="true">
      <Hairline />
      <MonogramSvg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 800 800"
        preserveAspectRatio="xMidYMid meet"
      >
        <text
          x="0"
          y="640"
          fill="none"
          stroke="rgba(245, 241, 234, 0.05)"
          strokeWidth="1.5"
          fontFamily='Georgia, "Playfair Display", "Times New Roman", serif'
          fontWeight={700}
          fontSize="780"
          letterSpacing="-0.06em"
        >
          CC
        </text>
      </MonogramSvg>
    </Outer>
  );
};
