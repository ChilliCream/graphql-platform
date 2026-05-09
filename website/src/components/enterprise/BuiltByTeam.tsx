"use client";

import React, { FC } from "react";

interface Maintainer {
  readonly key: string;
  readonly name: string;
  readonly initials: string;
  readonly handle: string;
  readonly role: string;
  readonly bio: string;
}

const MAINTAINERS: readonly Maintainer[] = [
  {
    key: "michael",
    name: "Michael Staib",
    initials: "MS",
    handle: "@michaelstaib",
    role: "Founder · Hot Chocolate lead maintainer",
    bio: "Hot Chocolate maintainer since the first commit. Long-time member of the GraphQL working group.",
  },
  {
    key: "pascal",
    name: "Pascal Senn",
    initials: "PS",
    handle: "@PascalSenn",
    role: "Co-founder · Fusion + Nitro lead",
    bio: "Designs and maintains Fusion's composition engine. Speaks at GraphQL Conf and runs the Discord.",
  },
  {
    key: "rafael",
    name: "Rafael Staib",
    initials: "RS",
    handle: "@rstaib",
    role: "Engineering · DX & tooling",
    bio: "Owns Strawberry Shake, Banana Cake Pop, and the developer experience surface across the platform.",
  },
  {
    key: "team",
    name: "ChilliCream core team",
    initials: "CC",
    handle: "@ChilliCream",
    role: "Maintainers, advocates, support",
    bio: "10+ engineers shipping Hot Chocolate, Fusion, and Nitro since 2019. The team that answers when something breaks.",
  },
];

export const BuiltByTeam: FC = () => {
  return (
    <section className="cc-ent-section cc-ent-team">
      <div className="cc-section-label">
        <span className="num">11</span> Authors
      </div>
      <div className="cc-ent-team-inner">
        <div className="cc-ent-heading">
          <div className="eyebrow">Built by the maintainers</div>
          <h2 className="display">Built by the team behind Hot Chocolate.</h2>
          <p>
            We've maintained Hot Chocolate since 2019 and contributed to the
            GraphQL specification across multiple working-group cycles. When you
            buy ChilliCream, the same engineers who ship the OSS are the ones on
            the call.
          </p>
        </div>
        <div className="cc-ent-team-grid">
          {MAINTAINERS.map((m) => (
            <article key={m.key} className="cc-ent-team-tile">
              <div className="cc-ent-team-mono">{m.initials}</div>
              <h3 className="cc-ent-team-name">{m.name}</h3>
              <p className="cc-ent-team-handle">{m.handle}</p>
              <p className="cc-ent-team-bio">
                <span style={{ color: "var(--cc-ink)" }}>{m.role}.</span>{" "}
                {m.bio}
              </p>
            </article>
          ))}
        </div>
        <p className="cc-ent-team-footer">
          Hot Chocolate has shipped continuously since 2019 — Apache 2.0
          licensed, on every major .NET release, and with a public roadmap on
          GitHub.
        </p>
      </div>
    </section>
  );
};
