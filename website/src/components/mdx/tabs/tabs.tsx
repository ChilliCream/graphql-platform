"use client";

import React, { createContext, FC, ReactNode, useContext } from "react";

import { useActiveTab } from "./tab-groups";

export interface TabsProps {
  readonly defaultValue?: string;
  readonly defaultvalue?: string;
  readonly groupId?: string;
  readonly groupid?: string;
  readonly children: ReactNode;
}

export const Tabs: FC<TabsProps> = ({
  defaultValue,
  defaultvalue,
  groupId,
  groupid,
  children,
}) => {
  const [activeTab, setActiveTab] = useActiveTab(
    (defaultValue || defaultvalue)!,
    groupId || groupid
  );

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
