import React, {
  createContext,
  FunctionComponent,
  ReactNode,
  useContext,
} from "react";
import { List } from "./list";
import { Panel, PanelProps } from "./panel";
import { Tab, TabProps } from "./tab";
import { useActiveTab, useIsClient } from "./tab-groups";

interface TabsContext {
  activeTab: string;
  setActiveTab: (value: string) => void;
}

export interface TabsComposition {
  Tab: FunctionComponent<TabProps>;
  Panel: FunctionComponent<PanelProps>;
  List: FunctionComponent;
}

const TabsContext = createContext<TabsContext | undefined>(undefined);

export interface TabsProps {
  defaultValue: string;
  groupId?: string;
  children: ReactNode;
}

export const Tabs: FunctionComponent<TabsProps> & TabsComposition = ({
  defaultValue,
  groupId,
  children,
}) => {
  const { key } = useIsClient();
  const [activeTab, setActiveTab] = useActiveTab(defaultValue, groupId);

  return (
    <TabsContext.Provider
      key={key}
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
