"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { Card } from "@/components/redesign-system/Card";
import { MIGRATION_PATHS } from "@/data/enterprise/migration-paths";

// Migration cards are paths, not constraints. Use `Card variant="ghost"` so
// they read as borderless content panels separated by hairlines, not as
// purchasable products. The colored top edges that used to make these
// indistinguishable from the SKUs above are gone.
export const MigrationSection: FC = () => {
  return (
    <Band variant="default" ariaLabel="Migration paths">
      <div className="cc-section-label">
        <span className="num">12</span> Migration
      </div>
      <div className="cc-ent-migration-inner">
        <div className="cc-ent-heading">
          <div className="eyebrow">Migration & enablement</div>
          <h2 className="display">
            Migrating from Apollo Federation, Hasura, or hand-rolled BFFs?
          </h2>
          <p>
            We run a paid two-week federation kickoff: schema audit,
            architecture review, a working Fusion mesh on a slice of your stack,
            and a written rollout plan. One named solution architect, no
            handoff.
          </p>
        </div>
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
