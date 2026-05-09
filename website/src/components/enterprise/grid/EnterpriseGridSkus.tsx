"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
import { SKUS } from "@/data/enterprise/skus";

// 3-up SKU card row. Each cell is a square hairline-bordered card with the
// SKU name (h3), one-line tagline, bullet list, and a "Read docs" arrow link
// at the bottom. Mirrors the `vercel-fluid` pricing-tier pattern with no
// rounded corners and no chrome gradients.

const Check: FC = () => (
  <svg viewBox="0 0 16 16" width="14" height="14" aria-hidden>
    <path
      d="M3 8.5 L6.5 12 L13 4.5"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  </svg>
);

const SkuCell = styled.div`
  display: flex;
  flex-direction: column;
  gap: 16px;
  height: 100%;
  min-height: 420px;
`;

const Tagline = styled.p`
  font-size: 14px;
  line-height: 1.5;
  color: var(--cc-ink-dim);
  margin: 0;
  text-wrap: pretty;
`;

const Bullets = styled.ul`
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
  flex: 1;

  li {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    font-size: 13px;
    color: var(--cc-ink);
    line-height: 1.5;
  }
  li svg {
    color: var(--cc-accent);
    flex-shrink: 0;
    margin-top: 3px;
  }
`;

const DocsLink = styled.a`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-ink);
  text-decoration: none;
  padding-top: 18px;
  border-top: 1px solid var(--cc-ink-faint);
  display: inline-flex;
  align-items: center;
  gap: 8px;
  transition: color 0.15s ease;

  &:hover {
    color: var(--cc-accent);
  }

  .cc-grid-arrow {
    color: var(--cc-accent);
  }
`;

export const EnterpriseGridSkus: FC = () => {
  return (
    <GridSection>
      <div className="cc-grid-section-head">
        <span className="cc-grid-eyebrow">Three named capabilities</span>
        <h2 className="cc-grid-h2">
          Three SKUs your bake-off matrix will recognise.
        </h2>
        <p>
          One noun per capability. Memorable, namespace-able, and easy to put on
          a vendor selection sheet next to whatever else you're evaluating.
        </p>
      </div>
      <GridRow cols={3}>
        {SKUS.map((sku) => (
          <GridCard key={sku.key} as="article">
            <SkuCell>
              <span className="cc-grid-eyebrow">{sku.name}</span>
              <h3 className="cc-grid-h3">{sku.tagline}</h3>
              <Tagline />
              <Bullets>
                {sku.bullets.map((b) => (
                  <li key={b}>
                    <Check />
                    <span>{b}</span>
                  </li>
                ))}
              </Bullets>
              <DocsLink href={sku.docsHref}>
                <span>{sku.docsLabel}</span>
                <span className="cc-grid-arrow" aria-hidden="true">
                  →
                </span>
              </DocsLink>
            </SkuCell>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};
