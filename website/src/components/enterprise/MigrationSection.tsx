"use client";

import React, { FC } from "react";

import { MIGRATION_PATHS } from "@/data/enterprise/migration-paths";

export const MigrationSection: FC = () => {
  return (
    <section className="cc-ent-section cc-ent-migration">
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
            <a
              key={path.key}
              href={path.ctaHref}
              className="cc-ent-migration-card"
            >
              <div className="cc-ent-migration-source">From {path.source}</div>
              <h3 className="cc-ent-migration-headline">{path.headline}</h3>
              <p className="cc-ent-migration-body">{path.body}</p>
              <span className="cc-ent-migration-cta">{path.cta} →</span>
            </a>
          ))}
        </div>
      </div>
    </section>
  );
};
