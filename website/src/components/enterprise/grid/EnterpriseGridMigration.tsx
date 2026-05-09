"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
import { MIGRATION_PATHS } from "@/data/enterprise/migration-paths";

// 3-up migration row (archetype D). One card per source platform with the
// "From X" eyebrow above an h3 headline, body paragraph, and a mono CTA
// link at the bottom. Borders shared via `<GridRow>` so the row reads as a
// single hairline frame.

const MigrationCell = styled.div`
  display: flex;
  flex-direction: column;
  gap: 14px;
  height: 100%;
  min-height: 280px;
`;

const Source = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, var(--cc-ink-dim));
`;

const Body = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: var(--cc-ink-dim);
  margin: 0;
  flex: 1;
  text-wrap: pretty;
`;

const Cta = styled.a`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-ink);
  text-decoration: none;
  margin-top: auto;
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

export const EnterpriseGridMigration: FC = () => {
  return (
    <GridSection>
      <div className="cc-grid-section-head">
        <span className="cc-grid-eyebrow">Migration and enablement</span>
        <h2 className="cc-grid-h2">
          Migrating from Apollo Federation, Hasura, or hand-rolled BFFs?
        </h2>
        <p>
          We run a paid two-week federation kickoff: schema audit, architecture
          review, a working Fusion mesh on a slice of your stack, and a written
          rollout plan. One named solution architect, no handoff.
        </p>
      </div>
      <GridRow cols={3}>
        {MIGRATION_PATHS.map((path) => (
          <GridCard key={path.key} as="article">
            <MigrationCell>
              <Source>From {path.source}</Source>
              <h3 className="cc-grid-h3">{path.headline}</h3>
              <Body>{path.body}</Body>
              <Cta href={path.ctaHref}>
                <span>{path.cta}</span>
                <span className="cc-grid-arrow" aria-hidden="true">
                  →
                </span>
              </Cta>
            </MigrationCell>
          </GridCard>
        ))}
      </GridRow>
    </GridSection>
  );
};
