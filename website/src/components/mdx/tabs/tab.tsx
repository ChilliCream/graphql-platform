import React, { FC } from "react";
import styled from "styled-components";

import { THEME_COLORS } from "@/style";
import { useTabs } from "./tabs";

export interface TabProps {
  readonly value: string;
}

export const Tab: FC<TabProps> = ({ value, children }) => {
  const { activeTab, setActiveTab } = useTabs();

  return (
    <TabButton
      className={activeTab === value ? "active" : undefined}
      onClick={() => setActiveTab(value)}
    >
      {children}
    </TabButton>
  );
};

const TabButton = styled.button`
  background-color: transparent;
  border: 0 none;
  cursor: pointer;
  padding: 1rem 0.25rem;
  font-size: 0.95rem;
  line-height: 1.2;
  color: ${THEME_COLORS.text};

  :hover {
    color: ${THEME_COLORS.heading};
  }

  :focus {
    outline: none;
  }

  &.active {
    color: ${THEME_COLORS.heading};
    font-weight: 600;
  }

  @media only screen and (min-width: 860px) {
    font-size: 18px;
  }
`;
