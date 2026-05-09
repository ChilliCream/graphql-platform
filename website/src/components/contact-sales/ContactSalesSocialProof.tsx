"use client";

import React, { FC } from "react";

interface ProofLine {
  readonly key: string;
  readonly persona: string;
  readonly metric: string;
}

const PROOF_LINES: readonly ProofLine[] = [
  {
    key: "eu-bank",
    persona: "EU retail bank",
    metric: "47 BFFs → 1 Fusion mesh, p99 480ms → 90ms",
  },
  {
    key: "logistics",
    persona: "Logistics PaaS",
    metric: "12-language polyglot federation on Nitro Self-Hosted",
  },
  {
    key: "fsi",
    persona: "FSI group",
    metric: "Air-gapped Nitro deployed in 6 weeks",
  },
];

export const ContactSalesSocialProof: FC = () => {
  return (
    <aside className="cc-cs-rail">
      <section className="cc-cs-proof" aria-label="Customer outcomes">
        <p className="cc-cs-proof-title">Recent outcomes</p>
        <ul className="cc-cs-proof-list">
          {PROOF_LINES.map((line) => (
            <li key={line.key} className="cc-cs-proof-item">
              <span className="cc-cs-proof-persona">{line.persona}</span>
              <span className="cc-cs-proof-metric">{line.metric}</span>
            </li>
          ))}
        </ul>
      </section>
      <section className="cc-cs-proof">
        <p className="cc-cs-proof-title">Quiet alternative</p>
        <p className="cc-cs-proof-alt">
          Prefer email? <strong>sales@chillicream.com</strong>. We don't run a
          phone tree or a live-chat widget — every reply comes from an engineer.
        </p>
      </section>
    </aside>
  );
};
