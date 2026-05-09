"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
import { COMPLIANCE_TILES, ComplianceKey } from "@/data/enterprise/compliance";

// Compliance grid (archetype D + dense capability strip). Top row = 3
// attestation tiles (SOC 2 / ISO / GDPR), bottom row = 5 capability tiles
// (SSO, RBAC, audit log, LTS, SLA). Both rows use shared 1px hairlines via
// `<GridRow>` so the whole compliance band reads as one continuous frame.

const stroke = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.5,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

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

const TierLabel = styled.div`
  display: inline-flex;
  align-items: center;
  gap: 10px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-ink-dim);
  margin: 0 0 18px;

  .num {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 22px;
    height: 22px;
    border: 1px solid var(--cc-ink-faint);
    color: var(--cc-ink);
  }
`;

const SecondTier = styled.div`
  margin-top: 56px;
`;

// 5-up shared-border grid for the capability tiles. `<GridRow>` only ships
// 2/3/4/6 column variants because those are the spec's primary widths;
// capability has 5 tiles so this is a one-off row composed by the same
// shared-border rules (border-top/left on container, border-right/bottom on
// children, mobile collapses to single column).
const CapGrid = styled.div`
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 0;
  width: 100%;
  border-top: 1px solid var(--cc-grid-hairline, rgba(245, 241, 234, 0.12));
  border-left: 1px solid var(--cc-grid-hairline, rgba(245, 241, 234, 0.12));
  overflow: hidden;

  > * {
    border: 0;
    border-right: 1px solid var(--cc-grid-hairline, rgba(245, 241, 234, 0.12));
    border-bottom: 1px solid var(--cc-grid-hairline, rgba(245, 241, 234, 0.12));
    border-radius: 0;
  }

  @media (max-width: 1024px) and (min-width: 721px) {
    grid-template-columns: repeat(2, 1fr);
  }

  @media (max-width: 720px) {
    grid-template-columns: 1fr;
    border-left: 0;

    > * {
      border-right: 0;
    }
  }
`;

const AttestCell = styled.a`
  display: flex;
  flex-direction: column;
  gap: 16px;
  height: 100%;
  min-height: 260px;
  text-decoration: none;
  color: inherit;

  &:hover .cc-attest-link {
    color: var(--cc-accent);
  }
`;

const AttestBadge = styled.div`
  width: 56px;
  height: 56px;
  border: 1px solid var(--cc-accent, var(--cc-ink-faint));
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--cc-accent);
  padding: 12px;
`;

const AttestTitle = styled.h3`
  font-size: 20px;
  font-weight: 500;
  letter-spacing: -0.015em;
  color: var(--cc-ink);
  margin: 0;
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
`;

const Status = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 9px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--cc-accent);
  padding: 3px 6px;
  border: 1px solid var(--cc-accent-line, var(--cc-ink-faint));
`;

const AttestBody = styled.p`
  font-size: 13px;
  line-height: 1.55;
  color: var(--cc-ink-dim);
  margin: 0;
  flex: 1;
  text-wrap: pretty;
`;

const AttestLink = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-ink);
  margin-top: auto;
  transition: color 0.15s ease;

  .cc-grid-arrow {
    color: var(--cc-accent);
    margin-left: 6px;
  }
`;

const CapCell = styled.a`
  display: flex;
  flex-direction: column;
  gap: 12px;
  height: 100%;
  min-height: 220px;
  text-decoration: none;
  color: inherit;
`;

const CapIcon = styled.div`
  width: 28px;
  height: 28px;
  color: var(--cc-accent);
`;

const CapTitle = styled.h4`
  font-size: 15px;
  font-weight: 500;
  letter-spacing: -0.01em;
  color: var(--cc-ink);
  margin: 0;
`;

const CapBody = styled.p`
  font-size: 13px;
  line-height: 1.5;
  color: var(--cc-ink-dim);
  margin: 0;
  text-wrap: pretty;
`;

const TrustLine = styled.p`
  margin: 36px auto 0;
  max-width: 720px;
  text-align: center;
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--cc-ink-dim);
  padding: 16px 18px;
  border: 1px solid var(--cc-ink-faint);

  strong {
    color: var(--cc-accent);
    font-weight: 500;
  }
`;

export const EnterpriseGridCompliance: FC = () => {
  const attestations = COMPLIANCE_TILES.filter((t) =>
    ATTESTATION_KEYS.includes(t.key)
  );
  const capabilities = COMPLIANCE_TILES.filter((t) =>
    CAPABILITY_KEYS.includes(t.key)
  );

  return (
    <GridSection>
      <div className="cc-grid-section-head">
        <span className="cc-grid-eyebrow">Compliance and trust</span>
        <h2 className="cc-grid-h2">Procurement-ready, end to end.</h2>
        <p>
          Every tile links to the document your security team will ask for. We
          sign DPAs, answer questionnaires under NDA, and ship audit evidence
          the same week you ask.
        </p>
      </div>

      <TierLabel>
        <span className="num">A</span>
        <span>Attestations</span>
      </TierLabel>
      <GridRow cols={3}>
        {attestations.map((tile) => (
          <GridCard key={tile.key}>
            <AttestCell href={tile.docsHref}>
              <AttestBadge>{COMPLIANCE_ICONS[tile.key]}</AttestBadge>
              <AttestTitle>
                {tile.title}
                {tile.status && <Status>{tile.status}</Status>}
              </AttestTitle>
              <AttestBody>{tile.body}</AttestBody>
              <AttestLink className="cc-attest-link">
                Read docs
                <span className="cc-grid-arrow" aria-hidden="true">
                  →
                </span>
              </AttestLink>
            </AttestCell>
          </GridCard>
        ))}
      </GridRow>

      <SecondTier>
        <TierLabel>
          <span className="num">B</span>
          <span>Capabilities</span>
        </TierLabel>
        <CapGrid>
          {capabilities.map((tile) => (
            <GridCard key={tile.key}>
              <CapCell href={tile.docsHref}>
                <CapIcon>{COMPLIANCE_ICONS[tile.key]}</CapIcon>
                <CapTitle>{tile.title}</CapTitle>
                <CapBody>{tile.body}</CapBody>
              </CapCell>
            </GridCard>
          ))}
        </CapGrid>
      </SecondTier>

      <TrustLine>
        <strong>Nitro Cloud uptime last 90 days: 99.98%.</strong> Live, fed by
        the same status backend as the public dashboard.
      </TrustLine>
    </GridSection>
  );
};
