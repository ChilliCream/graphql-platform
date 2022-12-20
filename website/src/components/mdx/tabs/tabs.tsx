import React, { createContext, FC, ReactNode, useContext } from "react";

import { useActiveTab } from "./tab-groups";

export interface TabsProps {
  readonly defaultValue: string;
  readonly groupId?: string;
  readonly children: ReactNode;
}

export const Tabs: FC<TabsProps> = ({ defaultValue, groupId, children }) => {
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

interface TabsContext {
  readonly activeTab: string;
  readonly setActiveTab: (value: string) => void;
}

const TabsContext = createContext<TabsContext | undefined>(undefined);

export const useTabs = (): TabsContext => {
  const context = useContext(TabsContext);
  if (!context) {
    throw new Error("This component must be used within a <Tabs> component.");
  }
  return context;
};
