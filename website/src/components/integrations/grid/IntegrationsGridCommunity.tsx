"use client";

import { useSearchParams } from "next/navigation";
import React, { FC, useMemo } from "react";
import styled from "styled-components";

import {
  GRID_TOKENS,
  GridRow,
  GridSection,
} from "@/components/redesign-system/grid";
import {
  communityIntegrations,
  recentlyAdded,
} from "@/data/integrations/integrations";

import { IntegrationGridCard } from "./IntegrationGridCard";

// Full grid of community integrations. Same shape as the Native row, denser
// (6-up instead of 4-up), with the dense card variant so the visual hierarchy
// communicates the trust tier without an extra badge: Native is heavier and
// more spacious; Community is smaller, denser, monogram-led.
//
// Hides itself when the active Type pill excludes Community.
export const IntegrationsGridCommunity: FC = () => {
  const searchParams = useSearchParams();
  const type = searchParams?.get("type");
  const q = (searchParams?.get("q") ?? "").trim().toLowerCase();

  const visible = useMemo(() => {
    if (type === "native") {
      return [];
    }
    const recent = new Set(recentlyAdded(6).map((i) => i.slug));
    return communityIntegrations().filter((i) => {
      if (type === "recent" && !recent.has(i.slug)) {
        return false;
      }
      if (q.length > 0) {
        const hay = (i.name + " " + i.tagline).toLowerCase();
        if (!hay.includes(q)) {
          return false;
        }
      }
      return true;
    });
  }, [type, q]);

  if (visible.length === 0) {
    return null;
  }

  return (
    <GridSection variant="default" hairlineTop>
      <Head>
        <HeadCopy>
          <Eyebrow>Built on the platform</Eyebrow>
          <Title>Community integrations.</Title>
          <Tagline>
            Open-source packages from the ecosystem. Maintained by the community
            and reviewed by us, listed once they ship a working release.
          </Tagline>
        </HeadCopy>
        <Count>
          {visible.length} {visible.length === 1 ? "package" : "packages"}
        </Count>
      </Head>
      <GridRow cols={6}>
        {visible.map((integration) => (
          <IntegrationGridCard
            key={integration.slug}
            integration={integration}
            dense
          />
        ))}
      </GridRow>
    </GridSection>
  );
};

const Head = styled.div`
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 24px;
  padding: 0 0 28px;
`;

const HeadCopy = styled.div`
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
`;

const Eyebrow = styled.span`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
`;

const Title = styled.h2`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(26px, 3vw, 38px);
  font-weight: 600;
  letter-spacing: -0.025em;
  line-height: 1.05;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0;
`;

const Tagline = styled.p`
  font-size: 14px;
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  margin: 0;
  max-width: 60ch;
`;

const Count = styled.span`
  flex: 0 0 auto;
  padding: 6px 12px;
  border: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: ${GRID_TOKENS.inkPrimary};
  white-space: nowrap;
`;
