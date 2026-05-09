"use client";

import Link from "next/link";
import React, { FC } from "react";
import styled from "styled-components";

import { productLabel, topologyLabel } from "@/data/templates/filters";
import { type Template } from "@/data/templates/templates";

import { TEMPLATE_THUMBNAILS, templateAccentVars } from "../TemplateCard";

// Square gallery card for the Grid variant. Shares the per-template SVG
// thumbnails and accent-token vocabulary with the Default `TemplateCard`,
// but renders inside a hairline-bordered 1:1 frame with no rounded corners,
// no drop shadow, and a flat surface. The card is the entire link target.
//
// Composition (top to bottom):
//   - Full-bleed thumbnail (60% of card height, accent-soft tint background)
//   - Title (h3, single line)
//   - 1-line tagline (clamped)
//   - Product chips
//   - "View" CTA arrow at the bottom-right

interface TemplatesGridCardProps {
  readonly template: Template;
}

export const TemplatesGridCard: FC<TemplatesGridCardProps> = ({ template }) => {
  const Thumb = TEMPLATE_THUMBNAILS[template.thumbnail];
  const style = templateAccentVars(template);
  return (
    <Card
      href={`/templates/${template.slug}`}
      style={style}
      aria-label={template.title}
    >
      <Thumbnail aria-hidden>
        <ThumbTag>{topologyLabel(template.topology)}</ThumbTag>
        {template.agentReady && <ThumbAgent>Agent-ready</ThumbAgent>}
        <Thumb />
      </Thumbnail>
      <Body>
        <Title>{template.title}</Title>
        <Tagline>{template.tagline}</Tagline>
        <Chips>
          {template.products.slice(0, 3).map((p) => (
            <Chip key={p}>{productLabel(p)}</Chip>
          ))}
        </Chips>
        <CtaRow>
          <Cta>
            View <Arrow aria-hidden>→</Arrow>
          </Cta>
        </CtaRow>
      </Body>
    </Card>
  );
};

const Card = styled(Link)`
  position: relative;
  display: flex;
  flex-direction: column;
  text-decoration: none;
  color: inherit;
  background: transparent;
  border-radius: 0;
  overflow: hidden;
  transition: background 0.18s ease;

  &:hover,
  &:focus-visible {
    background: rgba(255, 255, 255, 0.025);
  }
`;

// Square thumbnail occupies the upper portion of the card. Full-bleed
// (no inner frame), tinted by the per-template accent-soft color.
const Thumbnail = styled.div`
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 10;
  background: var(--cc-accent-soft);
  border-bottom: 1px solid var(--cc-grid-hairline, rgba(245, 241, 234, 0.16));
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;

  svg {
    width: 100%;
    height: 100%;
    display: block;
  }
`;

const ThumbTag = styled.span`
  position: absolute;
  top: 10px;
  left: 10px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--cc-ink, #f5f1ea);
  padding: 4px 8px;
  border: 1px solid var(--cc-accent-line);
  border-radius: 0;
  background: rgba(8, 14, 26, 0.7);
`;

const ThumbAgent = styled.span`
  position: absolute;
  top: 10px;
  right: 10px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: #0c1322;
  padding: 4px 8px;
  border-radius: 0;
  background: var(--cc-ink, #f5f1ea);
  font-weight: 600;
`;

const Body = styled.div`
  display: flex;
  flex-direction: column;
  flex: 1;
  gap: 10px;
  padding: 20px 22px 18px;
`;

const Title = styled.h3`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 18px;
  font-weight: 600;
  letter-spacing: -0.01em;
  line-height: 1.25;
  color: inherit;
  margin: 0;
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
`;

const Tagline = styled.p`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  line-height: 1.5;
  color: rgba(245, 241, 234, 0.62);
  margin: 0;
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
`;

const Chips = styled.div`
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding-top: 6px;
`;

const Chip = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 10px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: rgba(245, 241, 234, 0.8);
  padding: 4px 8px;
  border: 1px solid var(--cc-grid-hairline, rgba(245, 241, 234, 0.16));
  border-radius: 0;
  background: transparent;
  line-height: 1;
`;

const CtaRow = styled.div`
  display: flex;
  justify-content: flex-end;
  margin-top: auto;
  padding-top: 8px;
`;

const Cta = styled.span`
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  font-weight: 500;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--cc-accent, currentColor);
`;

const Arrow = styled.span`
  display: inline-block;
  transition: transform 0.18s ease;

  ${Card}:hover &,
  ${Card}:focus-visible & {
    transform: translateX(2px);
  }
`;
