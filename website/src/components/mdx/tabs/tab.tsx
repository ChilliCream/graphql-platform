import React, { FC } from "react";
import styled from "styled-components";
import { useTabs } from "./tabs";

export interface TabProps {
  value: string;
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
  color: var(--text-color);

  :hover {
    color: var(--heading-text-color);
  }

  :focus {
    outline: none;
  }

  &.active {
    color: var(--heading-text-color);
    font-weight: 700;
  }

  @media only screen and (min-width: 820px) {
    font-size: 18px;
  }
`;
