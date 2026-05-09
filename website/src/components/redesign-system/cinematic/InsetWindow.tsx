"use client";

import React, { useState } from "react";
import styled, { css } from "styled-components";

// Tabbed product-mockup window lifted from the homepage's `.cc-tab-panel-d` +
// `.cc-tab-grid` + `.cc-tab-text` + `.cc-tab-viz` (see
// `landing/desktop/DesktopLandingRoot.tsx` lines 268-437). A frosted dark
// inset with a tab strip on top and a 1.15fr/1fr text+viz grid below. The
// viz slot is a render prop so callers can drop in real waterfalls, code
// frames, or illustrations.

export interface InsetWindowTab {
  /** Stable identifier for the tab. */
  id: string;
  /** Visible tab label. */
  label: string;
}

export interface InsetWindowProps {
  /** Ordered list of tabs rendered along the top. */
  tabs: InsetWindowTab[];
  /** Controlled active tab id. When omitted, the component manages its own state. */
  activeTabId?: string;
  /** Fired when the user clicks a tab. */
  onChangeTab?: (id: string) => void;
  /** Heading rendered inside the text column. */
  title: string;
  /** Optional body copy. Plain text or a rich node. */
  body?: React.ReactNode;
  /** Optional terminal-style bullets rendered in the footer. */
  bullets?: string[];
  /** Render slot for the right-hand viz frame. */
  viz: React.ReactNode;
  className?: string;
}

const Outer = styled.div`
  background: linear-gradient(
    180deg,
    rgba(20, 28, 46, 0.8),
    rgba(12, 19, 34, 0.92)
  );
  border: 1px solid var(--cc-ink-faint);
  border-radius: 16px;
  padding: clamp(20px, 2.4vw, 28px);
  display: flex;
  flex-direction: column;
  gap: 18px;
`;

const TabBar = styled.div`
  display: flex;
  gap: 4px;
  padding: 4px;
  background: rgba(12, 19, 34, 0.7);
  backdrop-filter: blur(10px) saturate(110%);
  -webkit-backdrop-filter: blur(10px) saturate(110%);
  border: 1px solid var(--cc-ink-faint);
  border-radius: 14px;
  flex-wrap: wrap;
`;

interface TabButtonProps {
  $active: boolean;
}

const TabButton = styled.button<TabButtonProps>`
  flex: 1;
  min-width: max-content;
  padding: 10px 18px;
  border: none;
  border-radius: 10px;
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.18em;
  font-weight: 500;
  text-transform: uppercase;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
  white-space: nowrap;

  ${({ $active }) =>
    $active
      ? css`
          background: var(--cc-ink);
          color: #0c1322;
        `
      : css`
          background: transparent;
          color: var(--cc-ink-dim);

          &:hover {
            color: var(--cc-ink);
          }
        `}
`;

const Grid = styled.div`
  display: grid;
  grid-template-columns: minmax(0, 1.15fr) minmax(0, 1fr);
  gap: clamp(24px, 4vw, 56px);
  align-items: stretch;

  @media (max-width: 880px) {
    grid-template-columns: 1fr;
  }
`;

const TextColumn = styled.div`
  min-width: 0;
  display: flex;
  flex-direction: column;
`;

const Title = styled.h3`
  font-family: var(--cc-font-sans), sans-serif;
  font-size: clamp(22px, 2.4vw, 28px);
  font-weight: 500;
  letter-spacing: -0.02em;
  line-height: 1.15;
  color: var(--cc-ink);
  margin: 0 0 16px;
`;

const Body = styled.div`
  font-size: clamp(14px, 1.1vw, 16px);
  line-height: 1.6;
  color: var(--cc-ink-dim);
  text-wrap: pretty;
`;

const Footer = styled.div`
  margin-top: 32px;
  padding-top: 24px;
  border-top: 1px solid var(--cc-ink-faint);
`;

const BulletList = styled.ul`
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
`;

const Bullet = styled.li`
  font-family: var(--cc-font-mono), monospace;
  font-size: 11px;
  letter-spacing: 0.06em;
  color: var(--cc-ink);
  text-transform: uppercase;
  padding: 6px 10px;
  border: 1px solid var(--cc-ink-faint);
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.02);
`;

const VizFrame = styled.div`
  border: 1px dashed var(--cc-ink-faint);
  border-radius: 10px;
  background: radial-gradient(
      80% 60% at 50% 0%,
      rgba(255, 255, 255, 0.03),
      transparent 60%
    ),
    rgba(255, 255, 255, 0.015);
  min-height: 240px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: clamp(16px, 2vw, 24px);

  @media (max-width: 880px) {
    min-height: 160px;
  }
`;

/**
 * Frosted dark inset that frames a piece of product chrome, with a top tab
 * strip and a text+viz two-column body. The viz slot accepts arbitrary
 * children so the same shell can host code, traces, or illustrations.
 */
export const InsetWindow: React.FC<InsetWindowProps> = ({
  tabs,
  activeTabId,
  onChangeTab,
  title,
  body,
  bullets,
  viz,
  className,
}) => {
  const [internalActive, setInternalActive] = useState<string>(
    () => activeTabId ?? tabs[0]?.id ?? ""
  );
  const active = activeTabId ?? internalActive;

  const handleSelect = (id: string) => {
    if (activeTabId === undefined) {
      setInternalActive(id);
    }
    onChangeTab?.(id);
  };

  return (
    <Outer className={className}>
      <TabBar role="tablist">
        {tabs.map((tab) => (
          <TabButton
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={tab.id === active}
            $active={tab.id === active}
            onClick={() => handleSelect(tab.id)}
          >
            {tab.label}
          </TabButton>
        ))}
      </TabBar>
      <Grid>
        <TextColumn>
          <Title>{title}</Title>
          {body ? <Body>{body}</Body> : null}
          {bullets && bullets.length > 0 ? (
            <Footer>
              <BulletList>
                {bullets.map((b, i) => (
                  <Bullet key={i}>{b}</Bullet>
                ))}
              </BulletList>
            </Footer>
          ) : null}
        </TextColumn>
        <VizFrame>{viz}</VizFrame>
      </Grid>
    </Outer>
  );
};
