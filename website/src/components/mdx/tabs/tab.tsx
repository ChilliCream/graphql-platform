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
            className={activeTab === value ? 'active' : undefined}
            onClick={() => setActiveTab(value)}
        >
            {children}
        </TabButton>
    );
};

const TabButton = styled.button`
    padding: 1rem .25rem;
    font-size: 0.95rem;
    line-height: 1.2;
    color: #666677;
    border-bottom: 2px solid transparent;

    :hover {
        color: #2d2d35;
        border-color: #42424d;
    }

    &.active {
        color: #f40010;
        border-color: #f40010;
    }

    @media only screen and (min-width: 820px) {
      font-size: 1rem;
  }
`;
