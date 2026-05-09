"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { Band } from "@/components/redesign-system/Band";
import { Card } from "@/components/redesign-system/Card";
import { FrostedExplainer } from "@/components/redesign-system/cinematic";
import { MIGRATION_PATHS } from "@/data/enterprise/migration-paths";

// Cinematic variant of `MigrationSection`: same data, same migration card
// rack, but the lead body copy is wrapped in a `<FrostedExplainer>` plate
// so it picks up the homepage's frosted-glass treatment without covering
// the cards below it.

const HeadingWrap = styled.div`
  text-align: center;
  margin: 0 auto 48px;
  max-width: 760px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 14px;
`;

const Headline = styled.h2`
  font-size: clamp(34px, 4.4vw, 56px);
  margin: 8px auto 0;
  max-width: 22ch;
`;

// Tighter inner spacing on the explainer copy so the plate hugs the
// paragraph; the surrounding `HeadingWrap` owns vertical rhythm.
const ExplainerCopy = styled.p`
  font-size: clamp(15px, 1.1vw, 17px);
  color: var(--cc-ink-dim);
  margin: 0;
  text-wrap: pretty;
  line-height: 1.55;
`;

export const MigrationSectionCinematic: FC = () => {
  return (
    <Band variant="default" ariaLabel="Migration paths">
      <div className="cc-section-label">
        <span className="num">09</span> Migration
      </div>
      <div className="cc-ent-migration-inner">
        <HeadingWrap>
          <div className="eyebrow">Migration & enablement</div>
          <Headline className="display">
            Migrating from Apollo Federation, Hasura, or hand-rolled BFFs?
          </Headline>
          <FrostedExplainer maxWidth="60ch" tone="dark">
            <ExplainerCopy>
              We run a paid two-week federation kickoff: schema audit,
              architecture review, a working Fusion mesh on a slice of your
              stack, and a written rollout plan. One named solution architect,
              no handoff.
            </ExplainerCopy>
          </FrostedExplainer>
        </HeadingWrap>
        <div className="cc-ent-migration-grid">
          {MIGRATION_PATHS.map((path) => (
            <Card
              key={path.key}
              variant="ghost"
              as="article"
              className="cc-ent-migration-card"
            >
              <div className="cc-ent-migration-source">From {path.source}</div>
              <h3 className="cc-ent-migration-headline">{path.headline}</h3>
              <p className="cc-ent-migration-body">{path.body}</p>
              <a href={path.ctaHref} className="cc-ent-migration-cta">
                {path.cta} →
              </a>
            </Card>
          ))}
        </div>
      </div>
    </Band>
  );
};
