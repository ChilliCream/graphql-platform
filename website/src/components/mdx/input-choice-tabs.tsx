import React, { FC } from "react";

import { List, Panel, Tab, Tabs } from "./tabs";

export interface InputChoiceTabsComposition {
  CLI: FC;
  VisualStudio: FC;
}

export const InputChoiceTabs: FC & InputChoiceTabsComposition = ({
  children,
}) => {
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

const CLI: FC = ({ children }) => <Panel value="cli">{children}</Panel>;

const VisualStudio: FC = ({ children }) => (
  <Panel value="visual-studio">{children}</Panel>
);

InputChoiceTabs.CLI = CLI;
InputChoiceTabs.VisualStudio = VisualStudio;
