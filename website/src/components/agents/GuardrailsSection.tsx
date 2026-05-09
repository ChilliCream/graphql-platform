"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { Guardrail, GUARDRAILS, GuardrailIcon } from "@/data/agents/guardrails";

// Section 07: four guardrails. Tone is ops-grade, not marketing-soft. Each
// card pairs a stroke icon (in amber) with a serious one-liner. This is the
// ONE section on the page that keeps card chrome — the amber stroke is a
// system signal: "this card has a constraint." Everywhere else on the page,
// borders have been stripped.

const STROKE = {
  fill: "none" as const,
  stroke: "currentColor",
  strokeWidth: 1.6,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

const ICONS: Record<GuardrailIcon, () => React.ReactElement> = {
  // Schema-typed: braces + check.
  schema: () => (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <g {...STROKE}>
        <path d="M9 5 Q5 5 5 9 L5 11 Q5 12 4 12 Q5 12 5 13 L5 15 Q5 19 9 19" />
        <path d="M15 5 Q19 5 19 9 L19 11 Q19 12 20 12 Q19 12 19 13 L19 15 Q19 19 15 19" />
        <path d="M9.5 12 L11.5 14 L14.5 10" />
      </g>
    </svg>
  ),
  // Scoped tokens: key.
  token: () => (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <g {...STROKE}>
        <circle cx="9" cy="12" r="4" />
        <line x1="13" y1="12" x2="21" y2="12" />
        <line x1="17" y1="12" x2="17" y2="15" />
        <line x1="20" y1="12" x2="20" y2="14" />
      </g>
    </svg>
  ),
  // Audit log: stacked horizontal lines with a dot at the head.
  audit: () => (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <g {...STROKE}>
        <line x1="6" y1="6" x2="18" y2="6" />
        <line x1="6" y1="10" x2="14" y2="10" />
        <line x1="6" y1="14" x2="18" y2="14" />
        <line x1="6" y1="18" x2="12" y2="18" />
        <circle cx="20" cy="6" r="1.6" fill="currentColor" stroke="none" />
        <circle cx="16" cy="10" r="1.6" fill="currentColor" stroke="none" />
      </g>
    </svg>
  ),
  // Sandbox: bordered region with a small lock.
  sandbox: () => (
    <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden>
      <g {...STROKE}>
        <rect x="4" y="6" width="16" height="14" rx="2" />
        <path d="M9 6 L9 4 Q9 3 10 3 L14 3 Q15 3 15 4 L15 6" />
        <rect x="10" y="11" width="4" height="5" rx="1" />
        <line x1="11" y1="13" x2="13" y2="13" />
      </g>
    </svg>
  ),
};

export const GuardrailsSection: FC = () => {
  return (
    <Band variant="default" ariaLabel="Guardrails">
      <div className="cc-ag-band-inner">
        <div className="cc-section-label">
          <span className="num">07</span> Guardrails
        </div>
        <div className="cc-ag-feature-header">
          <div className="eyebrow">Autonomy with a leash</div>
          <h2 className="display">Bounded by the schema, audited by Mocha.</h2>
          <p>
            Agents move fastest when the rails are real. Every Nitro MCP
            interaction is typed against the live federated schema, scoped to an
            identity, and replayable from the audit log.
          </p>
        </div>

        <div className="cc-ag-guardrails">
          {GUARDRAILS.map((g: Guardrail) => {
            const Icon = ICONS[g.key];
            return (
              <div key={g.key} className="cc-ag-guardrail">
                <span className="cc-ag-guardrail-icon" aria-hidden>
                  <Icon />
                </span>
                <div>
                  <h4>{g.title}</h4>
                  <p>{g.body}</p>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </Band>
  );
};
