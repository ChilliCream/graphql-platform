"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import React, { FC, useCallback, useMemo } from "react";
import styled, { css } from "styled-components";

import { GRID_TOKENS, GridSection } from "@/components/redesign-system/grid";
import { INTEGRATIONS } from "@/data/integrations/integrations";

// Type-pill values mirror the Default hero. "all" is the no-filter default;
// "recent" is the pseudo-type that surfaces the latest 6 additions. Native and
// Community map to integration.type. URL key is ?type=, kept identical to the
// other variants so cross-variant deep-links keep working.
const TYPE_PILLS = [
  { key: "all", label: "All" },
  { key: "native", label: "Native" },
  { key: "community", label: "Community" },
  { key: "recent", label: "Recently Added" },
] as const;

type IntegrationTypePill = (typeof TYPE_PILLS)[number]["key"];

// Archetype A. Centered text-only hero band, eyebrow + h1 + sub + a row of
// square ghost chips for the Type filter. The chips write ?type= to the URL;
// the grids elsewhere on the page read that value out of useSearchParams, so
// back/forward navigation is filter-aware and shareable.
export const IntegrationsGridHero: FC = () => {
  const pathname = usePathname();
  const router = useRouter();
  const searchParams = useSearchParams();

  const activeType = (searchParams?.get("type") ??
    "all") as IntegrationTypePill;

  const counts = useMemo(() => {
    const total = INTEGRATIONS.length;
    let native = 0;
    let community = 0;
    for (const i of INTEGRATIONS) {
      if (i.type === "native") {
        native++;
      } else {
        community++;
      }
    }
    return { all: total, native, community, recent: 6 };
  }, []);

  const writeParam = useCallback(
    (key: string, value: string | null): void => {
      const params = new URLSearchParams(searchParams?.toString() ?? "");
      if (value && value.length > 0) {
        params.set(key, value);
      } else {
        params.delete(key);
      }
      const qs = params.toString();
      router.replace(qs ? `${pathname}?${qs}` : pathname, { scroll: false });
    },
    [pathname, router, searchParams]
  );

  const onPickType = useCallback(
    (key: IntegrationTypePill): void => {
      writeParam("type", key === "all" ? null : key);
    },
    [writeParam]
  );

  return (
    <GridSection variant="default" hairlineTop hairlineBottom>
      <Wrap>
        <Eyebrow>Integrations</Eyebrow>
        <Headline>Plug ChilliCream into the rest of your stack.</Headline>
        <SubHeadline>
          The API platform for humans and agents works with the auth,
          observability, messaging, data, and frontend tools you already run.
        </SubHeadline>
        <PillRow role="tablist" aria-label="Filter by type">
          {TYPE_PILLS.map((pill) => {
            const isActive = activeType === pill.key;
            return (
              <Pill
                key={pill.key}
                type="button"
                role="tab"
                aria-selected={isActive}
                $active={isActive}
                onClick={() => onPickType(pill.key)}
              >
                <span>{pill.label}</span>
                <Count $active={isActive}>{counts[pill.key]}</Count>
              </Pill>
            );
          })}
        </PillRow>
      </Wrap>
    </GridSection>
  );
};

const Wrap = styled.div`
  text-align: center;
  max-width: 920px;
  margin: 0 auto;
  padding: clamp(48px, 8vw, 96px) 0;
`;

const Eyebrow = styled.div`
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--cc-accent, ${GRID_TOKENS.inkMuted});
  margin-bottom: 28px;
`;

const Headline = styled.h1`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: ${GRID_TOKENS.heroSize};
  font-weight: 600;
  line-height: 1.02;
  letter-spacing: -0.04em;
  color: ${GRID_TOKENS.inkPrimary};
  margin: 0 0 24px;
  text-wrap: balance;
`;

const SubHeadline = styled.p`
  font-size: clamp(16px, 1.2vw, 19px);
  line-height: 1.55;
  color: ${GRID_TOKENS.inkBody};
  max-width: 60ch;
  margin: 0 auto 40px;
  text-wrap: pretty;
`;

const PillRow = styled.div`
  display: inline-flex;
  flex-wrap: wrap;
  gap: 0;
  border: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  background: var(--cc-grid-card-bg, ${GRID_TOKENS.bgCard});
`;

interface PillStyleProps {
  $active: boolean;
}

const Pill = styled.button<PillStyleProps>`
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 12px 20px;
  border: 0;
  border-radius: 0;
  background: transparent;
  color: ${GRID_TOKENS.inkBody};
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  cursor: pointer;
  transition: background 0.12s ease, color 0.12s ease;

  & + & {
    border-left: 1px solid var(--cc-grid-hairline, ${GRID_TOKENS.hairline});
  }

  &:hover {
    color: ${GRID_TOKENS.inkPrimary};
    background: var(--cc-grid-card-hover, ${GRID_TOKENS.bgHover});
  }

  ${({ $active }) =>
    $active
      ? css`
          color: ${GRID_TOKENS.bgBase};
          background: var(--cc-accent, #ffffff);

          &:hover {
            color: ${GRID_TOKENS.bgBase};
            background: var(--cc-accent, #ffffff);
            opacity: 0.92;
          }
        `
      : ""}
`;

const Count = styled.span<PillStyleProps>`
  font-size: 10px;
  letter-spacing: 0.12em;
  opacity: ${({ $active }) => ($active ? 0.85 : 0.6)};
`;
