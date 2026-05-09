"use client";

import React from "react";
import styled, { css } from "styled-components";

// Horizontal proof-strip primitive. Used on enterprise, customers, solutions,
// and as a hero-foot on pricing. NO card chrome, pure typography on a band.
//
// Each cell is a flex item that distributes evenly across the row. Cells are
// separated by a 1px vertical hairline on desktop, a 1px horizontal hairline
// on mobile. Numbers carry the visual weight (display type, oversized);
// labels are body-scale; attribution is a quiet uppercase monospace eyebrow.
//
// If `href` is set on an item, the entire cell is rendered as an anchor with
// a subtle hover state (the value picks up the page accent gradient on text).

export interface StatItem {
  /** The display number, e.g. "47", "8.2B", "−62%". */
  value: string;
  /** A single-line description sitting below the value. */
  label: string;
  /** Uppercase monospace eyebrow, e.g. "MICROSOFT COMMERCE". */
  attribution?: string;
  /** Optional case-study link. When set, the whole cell is a link. */
  href?: string;
}

export type StatRowAlign = "left" | "center";

export interface StatRowProps {
  /** Exactly 3 or 4 items. */
  items: StatItem[];
  align?: StatRowAlign;
  className?: string;
}

interface OuterProps {
  $align: StatRowAlign;
  $count: number;
}

const Outer = styled.div<OuterProps>`
  display: flex;
  flex-direction: row;
  align-items: stretch;
  width: 100%;
  ${({ $align }) =>
    $align === "center"
      ? css`
          justify-content: center;
          text-align: center;
        `
      : css`
          justify-content: flex-start;
          text-align: left;
        `}

  @media (max-width: 720px) {
    flex-direction: column;
    text-align: left;
  }
`;

const cellInteractive = css`
  cursor: pointer;
  text-decoration: none;
  color: inherit;
  transition: transform 0.18s ease;

  &:hover .cc-stat-value,
  &:focus-visible .cc-stat-value {
    background: var(
      --cc-accent-gradient,
      linear-gradient(120deg, var(--cc-accent, currentColor), currentColor)
    );
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
  }

  &:focus-visible {
    outline: 2px solid var(--cc-accent, currentColor);
    outline-offset: 4px;
    border-radius: 2px;
  }
`;

interface CellProps {
  $align: StatRowAlign;
  $isLink: boolean;
}

const Cell = styled.div<CellProps>`
  flex: 1 1 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 0 clamp(16px, 3vw, 32px);
  ${({ $align }) =>
    $align === "center"
      ? css`
          align-items: center;
        `
      : css`
          align-items: flex-start;
        `}

  & + & {
    border-left: 1px solid var(--cc-ink-faint);
  }

  @media (max-width: 720px) {
    align-items: flex-start;
    padding: 20px 0;

    & + & {
      border-left: none;
      border-top: 1px solid var(--cc-ink-faint);
    }
  }

  ${({ $isLink }) => ($isLink ? cellInteractive : "")}
`;

const Attribution = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-ink-faint);
`;

const Value = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(36px, 5vw, 64px);
  font-weight: 500;
  letter-spacing: -0.04em;
  line-height: 1;
  color: var(--cc-ink);
  font-feature-settings: "tnum" 1;
`;

const Label = styled.span`
  font-size: clamp(14px, 1.1vw, 16px);
  line-height: 1.4;
  color: var(--cc-ink-dim);
`;

const StatCell: React.FC<{ item: StatItem; align: StatRowAlign }> = ({
  item,
  align,
}) => {
  const inner = (
    <>
      {item.attribution ? <Attribution>{item.attribution}</Attribution> : null}
      <Value className="cc-stat-value">{item.value}</Value>
      <Label>{item.label}</Label>
    </>
  );

  if (item.href) {
    return (
      <Cell as="a" href={item.href} $align={align} $isLink>
        {inner}
      </Cell>
    );
  }

  return (
    <Cell $align={align} $isLink={false}>
      {inner}
    </Cell>
  );
};

/**
 * Horizontal stat strip: 3-4 cells distributed evenly with hairline
 * separators. Use directly inside a `<Band>`, never inside a `<Card>`.
 */
export const StatRow: React.FC<StatRowProps> = ({
  items,
  align = "left",
  className,
}) => {
  return (
    <Outer $align={align} $count={items.length} className={className}>
      {items.map((item, i) => (
        <StatCell key={i} item={item} align={align} />
      ))}
    </Outer>
  );
};
