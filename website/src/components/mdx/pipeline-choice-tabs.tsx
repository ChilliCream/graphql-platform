import React, { FC } from "react";

import { List, Panel, Tab, Tabs } from "./tabs";

export interface PipelineChoiceTabsComposition {
  GitHubAction: FC;
  CLI: FC;
}

export const PipelineChoiceTabs: FC & PipelineChoiceTabsComposition = ({
  children,
}) => {
  return (
    <Tabs defaultValue={"github-action"} groupId="pipeline-choice">
      <List>
        <Tab value="github-action">GitHub Action</Tab>
        <Tab value="cli">CLI</Tab>
      </List>
      {children}
    </Tabs>
  );
};

const GitHubAction: FC = ({ children }) => (
  <Panel value="github-action">{children}</Panel>
);

const CLI: FC = ({ children }) => <Panel value="cli">{children}</Panel>;

PipelineChoiceTabs.GitHubAction = GitHubAction;
PipelineChoiceTabs.CLI = CLI;
