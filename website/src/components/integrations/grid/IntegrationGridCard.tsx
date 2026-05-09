"use client";

import Link from "next/link";
import React, { FC } from "react";
import styled from "styled-components";

import { GRID_TOKENS } from "@/components/redesign-system/grid";
import { categoryLabel } from "@/data/integrations/categories";
import type { Integration } from "@/data/integrations/integrations";

// Single integration card, Grid variant. Square corners, 1px hairline border
// inherited from the surrounding `<GridRow>` / `<GridSplit>`, no shadows, no
// gradients. Stack from top to bottom: monogram tile, eyebrow (category),
// integration name, single-line pitch, "Read docs ->" arrow CTA in the page
// accent at the bottom.
//
// Two density modes:
//   default - 4-up grid cell (Native).
//   dense   - 6-up grid cell (Community), trimmed padding and smaller
//             monogram so the row reads as a denser secondary tier.

interface IntegrationGridCardProps {
  readonly integration: Integration;
  readonly dense?: boolean;
}

export const IntegrationGridCard: FC<IntegrationGridCardProps> = ({
  integration,
  dense = false,
}) => {
  return (
    <CardLink href={`/integrations/${integration.slug}`} $dense={dense}>
      <Mono $dense={dense}>{integration.letter}</Mono>
      <Eyebrow>{categoryLabel(integration.category)}</Eyebrow>
      <Name $dense={dense}>{integration.name}</Name>
      {!dense ? <Tagline>{integration.tagline}</Tagline> : null}
      <ReadDocs>
        Read docs <span aria-hidden>&rarr;</span>
      </ReadDocs>
    </CardLink>
  );
};

interface CardStyleProps {
  $dense: boolean;
}

const CardLink = styled(Link)<CardStyleProps>`
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: ${({ $dense }) => ($dense ? "20px" : "28px")};
  background: var(--cc-grid-card-bg, ${GRID_TOKENS.bgCard});
  color: ${GRID_TOKENS.inkPrimary};
  text-decoration: none;
  transition: background 0.12s ease;
  min-height: ${({ $dense }) => ($dense ? "180px" : "240px")};

  &:hover {
    background: var(--cc-grid-card-hover, ${GRID_TOKENS.bgHover});
  }

  &:hover [data-grid-card-arrow] {
    transform: translateX(2px);
  }
`;

const Mono = styled.span<CardStyleProps>`
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: ${({ $dense }) => ($dense ? "32px" : "44px")};
  height: ${({ $dense }) => ($dense ? "32px" : "44px")};
  border: 1px solid
    var(--cc-grid-hairline-strong, ${GRID_TOKENS.hairlineStrong});
  font-family: var(--cc-font-sans), sans-serif;
  font-size: ${({ $dense }) => ($dense ? "16px" : "20px")};
  font-weight: 600;
  letter-spacing: -0.02em;
  color: ${GRID_TOKENS.inkPrimary};
  margin-bottom: ${({ $dense }) => ($dense ? "2px" : "8px")};
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkMuted};
`;

const Name = styled.h3<CardStyleProps>`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: ${({ $dense }) => ($dense ? "15px" : "clamp(17px, 1.4vw, 20px)")};
  font-weight: 600;
  letter-spacing: -0.015em;
  line-height: 1.25;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
`;

const Tagline = styled.p`
  font-size: 13px;
  line-height: 1.5;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  flex: 1;
  text-wrap: pretty;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
`;

const ReadDocs = styled.span.attrs({ "data-grid-card-arrow": "true" })`
  margin-top: auto;
  padding-top: 12px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkPrimary});
  display: inline-flex;
  gap: 6px;
  align-items: center;
  transition: transform 0.15s ease;
`;
