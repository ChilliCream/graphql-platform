"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { TypographicMoment } from "@/components/redesign-system/illustrations";

interface Maintainer {
  readonly key: string;
  readonly name: string;
  readonly handle: string;
  readonly role: string;
  readonly bio: string;
}

const MAINTAINERS: readonly Maintainer[] = [
  {
    key: "michael",
    name: "Michael Staib",
    handle: "@michaelstaib",
    role: "Founder · Hot Chocolate lead maintainer",
    bio: "Hot Chocolate maintainer since the first commit. Long-time member of the GraphQL working group.",
  },
  {
    key: "pascal",
    name: "Pascal Senn",
    handle: "@PascalSenn",
    role: "Co-founder · Fusion + Nitro lead",
    bio: "Designs and maintains Fusion's composition engine. Speaks at GraphQL Conf and runs the Discord.",
  },
  {
    key: "rafael",
    name: "Rafael Staib",
    handle: "@rstaib",
    role: "Engineering · DX & tooling",
    bio: "Owns Strawberry Shake, Banana Cake Pop, and the developer experience surface across the platform.",
  },
  {
    key: "team",
    name: "ChilliCream core team",
    handle: "@ChilliCream",
    role: "Maintainers, advocates, support",
    bio: "10+ engineers shipping Hot Chocolate, Fusion, and Nitro since 2019. The team that answers when something breaks.",
  },
];

// Authority via a typographic moment: the OSS project's name set huge as the
// section's hero, with maintainer info below as inline editorial content.
// No card chrome, no 4-up monogram tile rack — that primitive belongs to the
// migration / SKU rows.
export const BuiltByTeam: FC = () => {
  return (
    <Band variant="tinted" ariaLabel="Built by the team behind Hot Chocolate">
      <div className="cc-ent-tint-scope">
        <div className="cc-section-label">
          <span className="num">11</span> Authors
        </div>
        <div className="cc-ent-team-inner">
          <div className="cc-ent-heading">
            <div className="eyebrow">Built by the maintainers</div>
          </div>

          <div className="cc-ent-team-typo">
            <TypographicMoment
              text="Hot Chocolate"
              variant="outline"
              size="huge"
            />
          </div>

          <p className="cc-ent-team-lede">
            We've maintained Hot Chocolate since 2019 and contributed to the
            GraphQL specification across multiple working-group cycles. When you
            buy ChilliCream, the same engineers who ship the OSS are the ones on
            the call.
          </p>

          <div className="cc-ent-team-stats">
            <div className="cc-ent-team-stat">
              <span className="cc-ent-team-stat-num">2019</span>
              <span className="cc-ent-team-stat-label">
                Continuous OSS releases
              </span>
            </div>
            <div className="cc-ent-team-stat">
              <span className="cc-ent-team-stat-num">10+</span>
              <span className="cc-ent-team-stat-label">
                Engineers on the platform
              </span>
            </div>
            <div className="cc-ent-team-stat">
              <span className="cc-ent-team-stat-num">Apache 2.0</span>
              <span className="cc-ent-team-stat-label">
                Licensed, public roadmap on GitHub
              </span>
            </div>
          </div>

          <div className="cc-ent-team-roster">
            {MAINTAINERS.map((m) => (
              <div key={m.key} className="cc-ent-team-row">
                <div className="cc-ent-team-name">{m.name}</div>
                <div className="cc-ent-team-handle">{m.handle}</div>
                <div className="cc-ent-team-role">{m.role}</div>
                <p className="cc-ent-team-bio">{m.bio}</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </Band>
  );
};
