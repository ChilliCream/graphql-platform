"use client";

import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import { ActLabel } from "@/components/redesign-system/cinematic";

// Wrapper that decorates an existing enterprise section with the homepage's
// chapter-marker chrome: an `<ActLabel>` pinned at top: 36px and enough
// vertical clearance on the inner band so the absolute label does not
// collide with the section's headline. The legacy `.cc-section-label`
// rendered by the wrapped section is hidden via CSS so the band reads with
// the cinematic eyebrow only.

export interface CinematicSectionProps {
  /** Two-digit chapter number, e.g. "04". */
  n: string;
  /** Chapter name, will be uppercased + letter-spaced by the label. */
  name: string;
  /** Section content (typically a `<Band>`-rendering component). */
  children: ReactNode;
  className?: string;
}

const Wrap = styled.div`
  position: relative;
  overflow: visible;

  /* Hide the legacy in-flow section label so the cinematic ActLabel is the
     only chapter marker visible on the band. */
  & .cc-section-label {
    display: none;
  }

  /* Bump the section's top padding so the absolute ActLabel (top: 36px)
     does not collide with the headline below it. */
  & > section {
    padding-top: clamp(96px, 11vw, 168px);
  }
`;

/**
 * Wraps an existing section component with a chapter-marker `<ActLabel>` and
 * extra top padding. Used by the cinematic variant of `/enterprise` to
 * chapter every band without duplicating the section internals.
 */
export const CinematicSection: FC<CinematicSectionProps> = ({
  n,
  name,
  children,
  className,
}) => {
  return (
    <Wrap className={className}>
      <ActLabel n={n} name={name} />
      {children}
    </Wrap>
  );
};
