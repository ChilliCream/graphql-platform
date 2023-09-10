import React, { FC } from "react";

import { List, Panel, Tab, Tabs } from "./tabs";

export interface ApiChoiceTabsComposition {
  MinimalApis: FC;
  Regular: FC;
}

export const ApiChoiceTabs: FC & ApiChoiceTabsComposition = ({ children }) => {
  return (
    <Tabs defaultValue={"minimal-apis"} groupId="api-choice">
      <List>
        <Tab value="minimal-apis">.NET 6</Tab>
        <Tab value="regular">.NET 5 or earlier</Tab>
      </List>
      {children}
    </Tabs>
  );
};

const MinimalApis: FC = ({ children }) => (
  <Panel value="minimal-apis">{children}</Panel>
);

const Regular: FC = ({ children }) => <Panel value="regular">{children}</Panel>;

ApiChoiceTabs.MinimalApis = MinimalApis;
ApiChoiceTabs.Regular = Regular;
