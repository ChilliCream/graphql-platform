import React, { FC } from "react";
import { Tabs } from "./tabs";

export interface InputChoiceTabsComposition {
  CLI: FC;
  VisualStudio: FC;
}

export const InputChoiceTabs: FC & InputChoiceTabsComposition = ({
  children,
}) => {
  return (
    <Tabs defaultValue={"cli"} groupId="input-choice">
      <Tabs.List>
        <Tabs.Tab value="cli">CLI</Tabs.Tab>
        <Tabs.Tab value="visual-studio">Visual Studio</Tabs.Tab>
      </Tabs.List>
      {children}
    </Tabs>
  );
};

const CLI: FC = ({ children }) => (
  <Tabs.Panel value="cli">{children}</Tabs.Panel>
);

const VisualStudio: FC = ({ children }) => (
  <Tabs.Panel value="visual-studio">{children}</Tabs.Panel>
);

InputChoiceTabs.CLI = CLI;
InputChoiceTabs.VisualStudio = VisualStudio;
