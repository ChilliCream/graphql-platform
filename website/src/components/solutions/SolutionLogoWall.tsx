"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import { LOGOS } from "@/data/solutions/shared";

interface SolutionLogoWallProps {
  readonly logos: readonly string[];
  readonly caption: string;
  readonly stepNumber: string;
}

// Section 09: the customer logo wall. Anonymised tier-coded customers and
// a few named brands sit in one rhythm: each tile is a typographic
// descriptor lockup (INDUSTRY · SCALE · REGION style for anonymous, brand
// wordmark for named). No monogram circles, no card chrome. Same approach
// as the customers page trust wall: the typographic treatment is the
// proof.
//
// Each anonymous label maps to a structured caption that surfaces the
// sector and scale tier, since "EU BANK" inside a generic blue circle
// reads as missing-asset placeholder. Named brands keep their wordmark.
const ANONYMOUS_DESCRIPTORS: Record<string, string> = {
  euTier1Bank: "TIER-1 BANK · DACH · 18M ACCOUNTS",
  top3EuInsurer: "TOP-3 INSURER · EU · 11 MARKETS",
  naHealthNetwork: "HEALTH NETWORK · NA · 47 SYSTEMS",
  logisticsPaaS: "LOGISTICS PAAS · GLOBAL · 12 LANGUAGES",
  fsiGroup: "FSI GROUP · NA · 9 WK ROLLOUT",
  iberianRetailBank: "RETAIL BANK · IBERIA · 100% IN-VPC",
  dachReinsurer: "REINSURER · DACH · GDPR-FIRST",
  nordicTelco: "TELCO · NORDIC · 1.4M SUBSCRIBERS",
  ukChallengerBank: "CHALLENGER BANK · UK · MOBILE-FIRST",
  globalCardNetwork: "CARD NETWORK · GLOBAL · 120K REQ/S",
};

export const SolutionLogoWall: FC<SolutionLogoWallProps> = ({
  logos,
  caption,
  stepNumber,
}) => {
  const resolved = logos
    .map((id) => LOGOS[id])
    .filter((l): l is NonNullable<typeof l> => l !== undefined);

  return (
    <Band variant="default" ariaLabel="Adopters">
      <div className="cc-sl-section cc-sl-logos">
        <div className="cc-section-label">
          <span className="num">{stepNumber}</span> Adopters
        </div>
        <div className="cc-sl-logos-inner">
          <p className="cc-sl-logos-caption">{caption}</p>
          <div className="cc-sl-logos-grid">
            {resolved.map((l) => (
              <div
                key={l.id}
                className={`cc-sl-logo-lockup${l.named ? " is-named" : ""}`}
              >
                {l.named ? (
                  <span className="cc-sl-logo-wordmark">{l.label}</span>
                ) : (
                  <span className="cc-sl-logo-descriptor">
                    {ANONYMOUS_DESCRIPTORS[l.id] ?? l.label.toUpperCase()}
                  </span>
                )}
              </div>
            ))}
          </div>
        </div>
      </div>
    </Band>
  );
};
