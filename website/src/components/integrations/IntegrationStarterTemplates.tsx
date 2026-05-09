"use client";

import Link from "next/link";
import React, { FC } from "react";

// Section 08: three starter projects pairing ChilliCream with a partner.
// Cards link straight into /templates with a topology pre-filter, so the
// integration page hands off to a working copy-paste flow rather than a dead
// "look at this" surface.
//
// We hand-author the three rather than over-engineer a join from
// templates.ts: the curation is editorial, not automatic.
const STARTERS = [
  {
    slug: "fusion-with-nitro-observability",
    title: "Fusion + Nitro Observability",
    stack: "Hot Chocolate · Fusion · Nitro · OpenTelemetry",
    blurb:
      "Three-service Fusion mesh with Nitro tracing wired in. The Operator's Window starter, ready to deploy.",
  },
  {
    slug: "agent-ready-api",
    title: "Agent-Ready API",
    stack: "Hot Chocolate · Nitro · MCP",
    blurb:
      "A solo Hot Chocolate service that talks GraphQL to humans and MCP to agents. Same auth, same schema.",
  },
  {
    slug: "multi-tenant-saas-starter",
    title: "Multi-tenant SaaS Starter",
    stack: "Hot Chocolate · Nitro · Auth0 · Postgres",
    blurb:
      "Per-tenant isolation, RBAC, audit log, and a Next.js admin console wired to the same GraphQL endpoint.",
  },
] as const;

export const IntegrationStarterTemplates: FC = () => {
  return (
    <section className="cc-in-section cc-in-starters">
      <div className="cc-section-label">
        <span className="num">08</span> Starters
      </div>
      <div className="cc-in-starters-inner">
        <div className="cc-in-starters-head">
          <span className="eyebrow">Starter templates</span>
          <h2 className="display">See the integrations working together.</h2>
          <p>
            Three opinionated starters pairing ChilliCream with the integrations
            above. Clone, run, customize.
          </p>
        </div>
        <div className="cc-in-starters-grid">
          {STARTERS.map((s) => (
            <Link
              key={s.slug}
              href={`/templates/${s.slug}`}
              className="cc-in-starter"
            >
              <span className="stack">{s.stack}</span>
              <h3>{s.title}</h3>
              <p>{s.blurb}</p>
              <span className="cta">Open template →</span>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
};
