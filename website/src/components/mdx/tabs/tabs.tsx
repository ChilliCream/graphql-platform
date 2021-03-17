import React, {
  createContext,
  FunctionComponent,
  ReactNode,
  useContext,
  useMemo,
  useState,
} from "react";
import { Tab, TabProps } from "./tab";
import { Panel, PanelProps } from "./panel";
import { List } from "./list";

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
  children: ReactNode;
}

export const Tabs: FunctionComponent<TabsProps> & TabsComposition = ({
  defaultValue,
  children,
}) => {
  const [activeTab, setActiveTab] = useState(defaultValue);

  const memoizedContextValue = useMemo(
    () => ({
      activeTab,
      setActiveTab,
    }),
    [activeTab, setActiveTab]
  );

  return (
    <TabsContext.Provider value={memoizedContextValue}>
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
