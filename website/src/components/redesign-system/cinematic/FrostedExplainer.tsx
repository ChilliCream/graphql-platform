"use client";

import React from "react";
import styled, { css } from "styled-components";

// Frosted-glass copy plate lifted from the homepage's `.cc-explainer`
// (see `landing/desktop/DesktopLandingRoot.tsx` lines 227-260). The plate
// keeps body copy legible over connector lines and lens beams while the
// soft `::before` halo dissolves the plate's edge into the band so it
// doesn't cut hard against the bright background underneath.
//
// Use only where there is something visually busy behind the copy
// (connector lines, accent threads, lens glare). On a flat band the plate
// reduces to a slightly-darker rectangle and adds noise.

export type FrostedExplainerTone = "dark" | "cream";

export interface FrostedExplainerProps {
  /** Inner content max-width (CSS value). Defaults to `56ch`. */
  maxWidth?: string;
  /** Plate palette. Defaults to `dark`. */
  tone?: FrostedExplainerTone;
  className?: string;
  children: React.ReactNode;
}

const toneStyles = (tone: FrostedExplainerTone) => {
  switch (tone) {
    case "cream":
      return css`
        background: rgba(248, 243, 235, 0.6);
        color: #1a1f2e;
        border: 1px solid rgba(26, 31, 46, 0.08);

        &::before {
          background: radial-gradient(
            ellipse 70% 90% at 50% 50%,
            rgba(248, 243, 235, 0.6) 0%,
            rgba(248, 243, 235, 0.32) 45%,
            transparent 80%
          );
        }
      `;
    case "dark":
    default:
      return css`
        background: rgba(12, 19, 34, 0.55);
        color: var(--cc-ink);
        border: 1px solid rgba(245, 241, 234, 0.08);

        &::before {
          background: radial-gradient(
            ellipse 70% 90% at 50% 50%,
            rgba(12, 19, 34, 0.55) 0%,
            rgba(12, 19, 34, 0.32) 45%,
            transparent 80%
          );
        }
      `;
  }
};

interface OuterProps {
  $tone: FrostedExplainerTone;
  $maxWidth: string;
}

const Outer = styled.div<OuterProps>`
  position: relative;
  display: inline-block;
  padding: 24px 28px;
  border-radius: 16px;
  backdrop-filter: blur(10px) saturate(110%);
  -webkit-backdrop-filter: blur(10px) saturate(110%);
  box-shadow: 0 8px 40px rgba(0, 0, 0, 0.18);

  ${({ $tone }) => toneStyles($tone)}

  &::before {
    content: "";
    position: absolute;
    inset: -28px -52px;
    border-radius: 28px;
    pointer-events: none;
    z-index: -1;
  }

  & > * {
    max-width: ${({ $maxWidth }) => $maxWidth};
  }
`;

/**
 * Frosted-glass copy plate with a soft halo. Reserved for body copy that
 * sits over a busy backdrop. The plate does not impose typography on its
 * children, only the surrounding chrome.
 */
export const FrostedExplainer: React.FC<FrostedExplainerProps> = ({
  maxWidth = "56ch",
  tone = "dark",
  className,
  children,
}) => {
  return (
    <Outer $tone={tone} $maxWidth={maxWidth} className={className}>
      {children}
    </Outer>
  );
};
