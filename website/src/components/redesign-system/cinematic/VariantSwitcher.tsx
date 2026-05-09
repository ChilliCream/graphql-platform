"use client";

import Link from "next/link";
import React from "react";
import styled, { css } from "styled-components";

// Floating dev/preview widget for hopping between page variants
// (e.g. `default` vs `cinematic`). Pinned to the bottom-right corner with a
// frosted pill of toggle buttons and a small "VARIANT" eyebrow above.

export interface VariantOption {
  /** Stable identifier, e.g. `default` or `cinematic`. */
  id: string;
  /** Visible toggle label. */
  label: string;
  /** Destination URL for this variant. */
  href: string;
}

export interface VariantSwitcherProps {
  /** Ordered toggle options. */
  options: VariantOption[];
  /** Identifier of the currently active option. */
  currentId: string;
  className?: string;
}

const Outer = styled.div`
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 50;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 6px;
  pointer-events: none;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-ink-faint);
`;

const Pill = styled.div`
  display: inline-flex;
  gap: 4px;
  padding: 4px;
  border-radius: 999px;
  background: rgba(12, 19, 34, 0.55);
  backdrop-filter: blur(12px) saturate(110%);
  -webkit-backdrop-filter: blur(12px) saturate(110%);
  border: 1px solid var(--cc-ink-faint);
  box-shadow: 0 14px 40px -22px rgba(0, 0, 0, 0.6);
  pointer-events: auto;
`;

interface ToggleProps {
  $active: boolean;
}

const Toggle = styled(Link)<ToggleProps>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 8px 16px;
  border-radius: 999px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  text-decoration: none;
  font-weight: 500;
  transition: transform 0.12s ease, background 0.12s ease,
    border-color 0.12s ease, color 0.12s ease;

  ${({ $active }) =>
    $active
      ? css`
          background: var(--cc-ink);
          color: #0c1322;
          border: 1px solid transparent;
        `
      : css`
          background: transparent;
          color: var(--cc-ink);
          border: 1px solid var(--cc-ink-faint);

          &:hover,
          &:focus-visible {
            transform: translateY(-1px);
            border-color: var(--cc-ink);
          }
        `}
`;

/**
 * Fixed-position pill that switches between page variants. Renders a small
 * eyebrow above a frosted toggle row; each option is a `<Link>` to its href.
 */
export const VariantSwitcher: React.FC<VariantSwitcherProps> = ({
  options,
  currentId,
  className,
}) => {
  return (
    <Outer className={className} aria-label="Variant switcher">
      <Eyebrow>Variant</Eyebrow>
      <Pill role="group">
        {options.map((option) => (
          <Toggle
            key={option.id}
            href={option.href}
            $active={option.id === currentId}
            aria-current={option.id === currentId ? "page" : undefined}
          >
            {option.label}
          </Toggle>
        ))}
      </Pill>
    </Outer>
  );
};
