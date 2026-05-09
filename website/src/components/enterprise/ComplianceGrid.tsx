"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { COMPLIANCE_TILES, ComplianceKey } from "@/data/enterprise/compliance";

const stroke = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.6,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

// Larger badge mark for Attestations (28px) and a smaller mark for the
// Capabilities row (20px). Same vocabulary, different scale.
const COMPLIANCE_ICONS: Record<ComplianceKey, React.ReactNode> = {
  soc2: (
    <svg viewBox="0 0 24 24" width="100%" height="100%" aria-hidden {...stroke}>
      <path d="M12 3 L20 6 V12 a8 9 0 0 1 -8 9 a8 9 0 0 1 -8 -9 V6 z" />
      <path d="M9 12 L11 14 L15 10" />
    </svg>
  ),
  iso27001: (
    <svg viewBox="0 0 24 24" width="100%" height="100%" aria-hidden {...stroke}>
      <circle cx="12" cy="12" r="8" />
      <path d="M4 12 h16" />
      <path d="M12 4 a10 12 0 0 1 0 16 a10 12 0 0 1 0 -16" />
    </svg>
  ),
  gdpr: (
    <svg viewBox="0 0 24 24" width="100%" height="100%" aria-hidden {...stroke}>
      <circle cx="12" cy="12" r="8" />
      <circle cx="9" cy="10" r="0.8" />
      <circle cx="14" cy="9" r="0.8" />
      <circle cx="16" cy="13" r="0.8" />
      <circle cx="9" cy="15" r="0.8" />
      <circle cx="13" cy="16" r="0.8" />
    </svg>
  ),
  "sso-saml": (
    <svg viewBox="0 0 24 24" width="100%" height="100%" aria-hidden {...stroke}>
      <circle cx="9" cy="9" r="3" />
      <path d="M4 19 a5 5 0 0 1 10 0" />
      <path d="M14 9 h7" />
      <path d="M19 7 L21 9 L19 11" />
    </svg>
  ),
  rbac: (
    <svg viewBox="0 0 24 24" width="100%" height="100%" aria-hidden {...stroke}>
      <circle cx="8" cy="9" r="2.6" />
      <circle cx="16" cy="9" r="2.6" />
      <path d="M3 18 a5 5 0 0 1 10 0" />
      <path d="M11 18 a5 5 0 0 1 10 0" />
    </svg>
  ),
  "audit-log": (
    <svg viewBox="0 0 24 24" width="100%" height="100%" aria-hidden {...stroke}>
      <path d="M5 4 h11 l4 4 v12 a1 1 0 0 1 -1 1 H5 a1 1 0 0 1 -1 -1 V5 a1 1 0 0 1 1 -1 z" />
      <path d="M15 4 v5 h5" />
      <path d="M8 13 h8" />
      <path d="M8 16 h6" />
    </svg>
  ),
  lts: (
    <svg viewBox="0 0 24 24" width="100%" height="100%" aria-hidden {...stroke}>
      <circle cx="12" cy="12" r="8" />
      <path d="M12 7 V12 L15 14" />
    </svg>
  ),
  sla: (
    <svg viewBox="0 0 24 24" width="100%" height="100%" aria-hidden {...stroke}>
      <path d="M3 14 L8 9 L12 13 L21 4" />
      <path d="M15 4 h6 v6" />
    </svg>
  ),
};

const ATTESTATION_KEYS: readonly ComplianceKey[] = ["soc2", "iso27001", "gdpr"];
const CAPABILITY_KEYS: readonly ComplianceKey[] = [
  "sso-saml",
  "rbac",
  "audit-log",
  "lts",
  "sla",
];

// Two-tier compliance: top row = 3 large attestation badges (procurement
// triage), bottom row = dense capability grid (security review). Same data
// source, two primitives, mirrors enterprise.md §D.3.
export const ComplianceGrid: FC = () => {
  const attestations = COMPLIANCE_TILES.filter((t) =>
    ATTESTATION_KEYS.includes(t.key)
  );
  const capabilities = COMPLIANCE_TILES.filter((t) =>
    CAPABILITY_KEYS.includes(t.key)
  );

  return (
    <Band variant="accent" ariaLabel="Compliance and trust">
      <div className="cc-section-label">
        <span className="num">10</span> Compliance
      </div>
      <div className="cc-ent-compliance-inner">
        <div className="cc-ent-heading">
          <div className="eyebrow">Compliance & trust</div>
          <h2 className="display">Procurement-ready, end to end.</h2>
          <p>
            Every tile links to the document your security team will ask for. We
            sign DPAs, answer questionnaires under NDA, and ship audit evidence
            the same week you ask.
          </p>
        </div>

        {/* Tier 1: Attestations — three oversized badge tiles. */}
        <div className="cc-ent-attest-label">
          <span className="num">A</span> Attestations
        </div>
        <div className="cc-ent-attest-grid">
          {attestations.map((tile) => (
            <a
              key={tile.key}
              href={tile.docsHref}
              className="cc-ent-attest-tile"
            >
              <div className="cc-ent-attest-badge">
                {COMPLIANCE_ICONS[tile.key]}
              </div>
              <div className="cc-ent-attest-meta">
                <h3 className="cc-ent-attest-title">
                  {tile.title}
                  {tile.status && (
                    <span className="cc-ent-compliance-status">
                      {tile.status}
                    </span>
                  )}
                </h3>
                <p className="cc-ent-attest-body">{tile.body}</p>
                <span className="cc-ent-compliance-link">Read docs →</span>
              </div>
            </a>
          ))}
        </div>

        {/* Tier 2: Capabilities — dense 4-up grid. */}
        <div className="cc-ent-cap-label">
          <span className="num">B</span> Capabilities
        </div>
        <div className="cc-ent-cap-grid">
          {capabilities.map((tile) => (
            <a key={tile.key} href={tile.docsHref} className="cc-ent-cap-tile">
              <div className="cc-ent-cap-icon">
                {COMPLIANCE_ICONS[tile.key]}
              </div>
              <h4 className="cc-ent-cap-title">{tile.title}</h4>
              <p className="cc-ent-cap-body">{tile.body}</p>
            </a>
          ))}
        </div>

        <div className="cc-ent-compliance-trust">
          <strong>Nitro Cloud uptime last 90 days: 99.98%.</strong> Live, fed by
          the same status backend as the public dashboard.
        </div>
      </div>
    </Band>
  );
};
