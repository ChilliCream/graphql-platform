"use client";

import Link from "next/link";
import React from "react";
import styled, { css } from "styled-components";

// Blog-tile primitive inspired by the homepage's vibrant zine cards (orange,
// yellow ray-burst, pink). Each variant pairs a bold flat color or gradient
// with an optional decorative motif rendered in the upper right. Tiles are
// 16/9, 20px-radius, with a small lift on hover.

export type VibrantVariant =
  | "orange"
  | "yellow-rays"
  | "pink"
  | "gradient-prism";

export interface VibrantTileProps {
  /** Visual treatment. Determines background and default decoration. */
  variant: VibrantVariant;
  /** Tile headline rendered in display weight. */
  title: string;
  /** Optional small uppercase eyebrow rendered above the title. */
  tag?: string;
  /** Optional href. Renders the tile as a `<Link>` when present. */
  href?: string;
  /** Optional decorative SVG node placed in the upper-right corner. */
  imageMotif?: React.ReactNode;
  className?: string;
}

interface TileProps {
  $variant: VibrantVariant;
  $interactive: boolean;
}

const variantBackground = (variant: VibrantVariant) => {
  switch (variant) {
    case "orange":
      return css`
        background: oklch(0.74 0.2 35);
        color: var(--cc-ink);
      `;
    case "yellow-rays":
      return css`
        background: oklch(0.85 0.18 90);
        color: #1c1308;
      `;
    case "pink":
      return css`
        background: oklch(0.74 0.18 350);
        color: #1c0a18;
      `;
    case "gradient-prism":
    default:
      return css`
        background: conic-gradient(
          from 220deg at 50% 50%,
          var(--cc-col-cat, oklch(0.74 0.18 30)),
          var(--cc-col-bil, oklch(0.82 0.16 90)),
          var(--cc-col-ord, oklch(0.76 0.16 150)),
          var(--cc-col-shi, oklch(0.74 0.14 220)),
          var(--cc-col-cat, oklch(0.74 0.18 30))
        );
        color: var(--cc-ink);
      `;
  }
};

const Tile = styled.div<TileProps>`
  position: relative;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  aspect-ratio: 16 / 9;
  border-radius: 20px;
  padding: 28px;
  overflow: hidden;
  text-decoration: none;
  isolation: isolate;
  transition: transform 0.18s ease, filter 0.18s ease;

  ${({ $variant }) => variantBackground($variant)}

  ${({ $interactive }) =>
    $interactive
      ? css`
          cursor: pointer;

          &:hover,
          &:focus-visible {
            transform: translateY(-2px);
            filter: brightness(1.05);
          }

          &:focus-visible {
            outline: 2px solid var(--cc-ink);
            outline-offset: 4px;
          }
        `
      : ""}
`;

const Tag = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  font-weight: 600;
  opacity: 0.8;
`;

const Title = styled.span`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(18px, 1.6vw, 24px);
  font-weight: 600;
  letter-spacing: -0.015em;
  line-height: 1.15;
  text-wrap: pretty;
`;

const Motif = styled.div`
  position: absolute;
  top: 0;
  right: 0;
  width: 50%;
  height: 100%;
  pointer-events: none;
  display: flex;
  align-items: flex-start;
  justify-content: flex-end;

  & > svg {
    width: 100%;
    height: 100%;
  }
`;

const OrangeMotif: React.FC = () => (
  <svg viewBox="0 0 200 200" fill="none" aria-hidden="true">
    <text
      x="170"
      y="80"
      textAnchor="end"
      fontFamily="var(--cc-font-sans), sans-serif"
      fontSize="160"
      fontWeight="700"
      fill="rgba(245, 241, 234, 0.18)"
    >
      A
    </text>
    <circle cx="160" cy="150" r="22" fill="rgba(245, 241, 234, 0.18)" />
  </svg>
);

const YellowRaysMotif: React.FC = () => (
  <svg viewBox="0 0 200 200" fill="none" aria-hidden="true">
    {Array.from({ length: 14 }, (_, i) => {
      const angle = (i / 14) * Math.PI * 2;
      const x1 = 200 + Math.cos(angle) * 16;
      const y1 = 60 + Math.sin(angle) * 16;
      const x2 = 200 + Math.cos(angle) * 220;
      const y2 = 60 + Math.sin(angle) * 220;
      return (
        <line
          key={i}
          x1={x1}
          y1={y1}
          x2={x2}
          y2={y2}
          stroke="rgba(28, 19, 8, 0.35)"
          strokeWidth="2"
          strokeLinecap="round"
        />
      );
    })}
    <circle cx="200" cy="60" r="14" fill="rgba(28, 19, 8, 0.55)" />
  </svg>
);

const PinkMotif: React.FC = () => (
  <svg viewBox="0 0 200 200" fill="none" aria-hidden="true">
    <defs>
      <radialGradient id="cc-vt-pink-glow" cx="0.7" cy="0.4" r="0.6">
        <stop offset="0" stopColor="rgba(255, 240, 248, 0.7)" />
        <stop offset="0.6" stopColor="rgba(255, 240, 248, 0.15)" />
        <stop offset="1" stopColor="rgba(255, 240, 248, 0)" />
      </radialGradient>
    </defs>
    <circle cx="140" cy="80" r="120" fill="url(#cc-vt-pink-glow)" />
  </svg>
);

const defaultMotif = (variant: VibrantVariant): React.ReactNode | null => {
  switch (variant) {
    case "orange":
      return <OrangeMotif />;
    case "yellow-rays":
      return <YellowRaysMotif />;
    case "pink":
      return <PinkMotif />;
    case "gradient-prism":
    default:
      return null;
  }
};

/**
 * Bold-color blog-style tile with optional decorative motif. Use sparingly:
 * the tiles are the playful payoff at the end of a narrative, not the spine.
 */
export const VibrantTile: React.FC<VibrantTileProps> = ({
  variant,
  title,
  tag,
  href,
  imageMotif,
  className,
}) => {
  const motif = imageMotif ?? defaultMotif(variant);
  const interactive = Boolean(href);

  const inner = (
    <>
      {motif ? <Motif>{motif}</Motif> : null}
      {tag ? <Tag>{tag}</Tag> : <span aria-hidden="true" />}
      <Title>{title}</Title>
    </>
  );

  if (href) {
    return (
      <Tile
        as={Link}
        href={href}
        $variant={variant}
        $interactive
        className={className}
      >
        {inner}
      </Tile>
    );
  }

  return (
    <Tile $variant={variant} $interactive={interactive} className={className}>
      {inner}
    </Tile>
  );
};
