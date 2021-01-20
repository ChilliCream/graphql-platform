import React, { createContext, FC, useContext, useMemo, useState } from 'react';
import { Tab, TabProps } from './tab';
import { Panel, PanelProps } from './panel';
import { List } from './list';
import styled from 'styled-components';

interface TabsContext {
    activeTab: string;
    setActiveTab: (value: string) => void;
}

export interface TabsComposition {
    Tab: FC<TabProps>;
    Panel: FC<PanelProps>;
    List: FC;
}

const TabsContext = createContext<TabsContext | undefined>(undefined);

export interface TabsProps {
    defaultValue: string;
}

export const Tabs: FC<TabsProps> & TabsComposition = ({
    defaultValue, children
}) => {
    const [activeTab, setActiveTab] = useState(defaultValue);

    const memoizedContextValue = useMemo(
        () => ({
            activeTab,
            setActiveTab,
        }),
        [activeTab, setActiveTab],
    );

    return (
        <TabsContext.Provider value={memoizedContextValue}>
            {children}
            <ClosingBorder />
        </TabsContext.Provider>
    );
};

const ClosingBorder = styled.div`
  border-bottom: 1px solid #cacada;
  margin-bottom: 1rem;
`;

export const useTabs = (): TabsContext => {
    const context = useContext(TabsContext);
    if (!context) {
        throw new Error('This component must be used within a <Tabs> component.');
    }
    return context;
};

Tabs.Tab = Tab;
Tabs.Panel = Panel;
Tabs.List = List;
