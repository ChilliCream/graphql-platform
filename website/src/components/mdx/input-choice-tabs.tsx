import React, { FC } from "react";

import { List, Panel, Tab, Tabs } from "./tabs";

export interface InputChoiceTabsComposition {
  CLI: FC<{ children?: React.ReactNode }>;
  VisualStudio: FC<{ children?: React.ReactNode }>;
}

export const InputChoiceTabs: FC<{ children: React.ReactNode }> &
  InputChoiceTabsComposition = ({ children }) => {
  return (
    <Tabs defaultValue={"cli"} groupId="input-choice">
      <List>
        <Tab value="cli">CLI</Tab>
        <Tab value="visual-studio">Visual Studio</Tab>
      </List>
      {children}
    </Tabs>
  );
};

const CLI: FC<{ children?: React.ReactNode }> = ({ children }) => (
  <Panel value="cli">{children}</Panel>
);

const VisualStudio: FC<{ children?: React.ReactNode }> = ({ children }) => (
  <Panel value="visual-studio">{children}</Panel>
);

InputChoiceTabs.CLI = CLI;
InputChoiceTabs.VisualStudio = VisualStudio;
