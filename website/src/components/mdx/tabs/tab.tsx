import React, { FunctionComponent } from "react";
import styled from "styled-components";
import { useTabs } from "./tabs";

export interface TabProps {
  value: string;
}

export const Tab: FunctionComponent<TabProps> = ({ value, children }) => {
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
  color: #666677;

  :hover {
    color: #2d2d35;
  }

  :focus {
    outline: none;
  }

  &.active {
    color: #000;
    font-weight: 700;
  }

  @media only screen and (min-width: 820px) {
    font-size: 18px;
  }
`;
