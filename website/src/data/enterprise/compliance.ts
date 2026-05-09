// Eight-tile compliance grid (Section 10). This is the procurement-team
// section: every tile is a card with a title, a 1-2 sentence body, and a
// "Read docs" link that procurement can click through. Doc links are
// placeholders for now — wire them to the trust portal when it lands.

export type ComplianceKey =
  | "soc2"
  | "iso27001"
  | "gdpr"
  | "sso-saml"
  | "rbac"
  | "audit-log"
  | "lts"
  | "sla";

export interface ComplianceTile {
  readonly key: ComplianceKey;
  readonly title: string;
  readonly status?: string;
  readonly body: string;
  readonly docsHref: string;
}

export const COMPLIANCE_TILES: readonly ComplianceTile[] = [
  {
    key: "soc2",
    title: "SOC 2 Type II",
    body: "Independently audited controls covering security, availability, and confidentiality across Nitro Cloud and Nitro Self-Hosted. Latest report available under NDA.",
    docsHref: "#",
  },
  {
    key: "iso27001",
    title: "ISO 27001",
    status: "in progress",
    body: "Stage 2 certification audit scheduled. Statement of Applicability and current control matrix available under NDA.",
    docsHref: "#",
  },
  {
    key: "gdpr",
    title: "GDPR",
    body: "DPA, subprocessor list, EU data residency for Nitro Cloud, and a public Article 28 statement covering the platform.",
    docsHref: "#",
  },
  {
    key: "sso-saml",
    title: "SSO / SAML",
    body: "SAML 2.0 and OIDC against Okta, Entra ID, Google Workspace, and any IdP that speaks the standard. Just-in-time provisioning supported.",
    docsHref: "#",
  },
  {
    key: "rbac",
    title: "RBAC",
    body: "Role-based access on schemas, environments, and rollouts. Custom roles for review boards, on-call rotations, and external auditors.",
    docsHref: "#",
  },
  {
    key: "audit-log",
    title: "Audit log export",
    body: "Tamper-evident, structured audit log of every schema change, deploy, RBAC mutation, and access event. Streams to your SIEM.",
    docsHref: "#",
  },
  {
    key: "lts",
    title: "LTS support",
    body: "Long-term support branches of Hot Chocolate and Fusion with 24-month security backports. Built for regulated industries on slow upgrade cycles.",
    docsHref: "#",
  },
  {
    key: "sla",
    title: "99.9% SLA",
    body: "Contractual uptime SLA on Nitro Cloud and on Nitro Self-Hosted with the 24x7 oncall add-on. Service credits paid against the contract month.",
    docsHref: "#",
  },
];
