import React, { createContext, FC, ReactNode, useContext } from "react";
import { List } from "./list";
import { Panel, PanelProps } from "./panel";
import { Tab, TabProps } from "./tab";
import { useActiveTab } from "./tab-groups";

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
  readonly defaultValue: string;
  readonly groupId?: string;
  readonly children: ReactNode;
}

export const Tabs: FC<TabsProps> & TabsComposition = ({
  defaultValue,
  groupId,
  children,
}) => {
  const [activeTab, setActiveTab] = useActiveTab(defaultValue, groupId);

  return (
    <TabsContext.Provider
      value={{
        activeTab,
        setActiveTab,
      }}
    >
      {children}
    </TabsContext.Provider>
  );
};

export const useTabs = (): TabsContext => {
  const context = useContext(TabsContext);
  if (!context) {
    throw new Error("This component must be used within a <Tabs> component.");
  }
  return context;
};

Tabs.Tab = Tab;
Tabs.Panel = Panel;
Tabs.List = List;
