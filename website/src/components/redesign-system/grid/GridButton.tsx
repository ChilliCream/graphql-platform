"use client";

import React from "react";
import styled, { css } from "styled-components";

import { GRID_TOKENS } from "./tokens";

// Strict square-cornered button for the Grid variant. Three variants:
//   primary   - white background, dark text
//   secondary - hairline border, cream text on transparent ground
//   ghost     - borderless, with a trailing chevron glyph

export type GridButtonVariant = "primary" | "secondary" | "ghost";

export interface GridButtonProps {
  variant?: GridButtonVariant;
  href?: string;
  onClick?: () => void;
  children: React.ReactNode;
  className?: string;
}

const variantStyles = (variant: GridButtonVariant) => {
  switch (variant) {
    case "secondary":
      return css`
        background: transparent;
        color: ${GRID_TOKENS.inkPrimary};
        border: 1px solid
          var(--cc-grid-hairline-strong, ${GRID_TOKENS.hairlineStrong});

        &:hover,
        &:focus-visible {
          background: rgba(245, 241, 234, 0.04);
        }
      `;
    case "ghost":
      return css`
        background: transparent;
        color: ${GRID_TOKENS.inkPrimary};
        border: 0;
        padding-left: 0;
        padding-right: 0;

        &:hover,
        &:focus-visible {
          color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
        }
      `;
    case "primary":
    default:
      return css`
        background: #ffffff;
        color: ${GRID_TOKENS.bgBase};
        border: 1px solid #ffffff;

        &:hover,
        &:focus-visible {
          background: #e5e5e5;
          border-color: #e5e5e5;
        }
      `;
  }
};

interface ButtonProps {
  $variant: GridButtonVariant;
}

const Button = styled.button<ButtonProps>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 12px 22px;
  border-radius: 0;
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  font-weight: 500;
  line-height: 1;
  text-decoration: none;
  cursor: pointer;
  transition: background 0.12s ease, color 0.12s ease, border-color 0.12s ease;

  &:focus-visible {
    outline: 2px solid var(--cc-accent, ${GRID_TOKENS.inkPrimary});
    outline-offset: 2px;
  }

  ${({ $variant }) => variantStyles($variant)}
`;

const Chevron = styled.span`
  display: inline-block;
  transition: transform 0.18s ease;

  ${Button}:hover &,
  ${Button}:focus-visible & {
    transform: translateX(2px);
  }
`;

/**
 * Square-cornered button. `primary` is white-on-dark, `secondary` is a
 * hairline-bordered transparent pill, `ghost` is borderless with a trailing
 * chevron. Renders as an anchor when `href` is set, a button otherwise.
 */
export const GridButton: React.FC<GridButtonProps> = ({
  variant = "primary",
  href,
  onClick,
  children,
  className,
}) => {
  const content = (
    <>
      <span>{children}</span>
      {variant === "ghost" ? <Chevron aria-hidden="true">→</Chevron> : null}
    </>
  );

  if (href) {
    return (
      <Button as="a" href={href} $variant={variant} className={className}>
        {content}
      </Button>
    );
  }

  return (
    <Button
      type="button"
      onClick={onClick}
      $variant={variant}
      className={className}
    >
      {content}
    </Button>
  );
};
