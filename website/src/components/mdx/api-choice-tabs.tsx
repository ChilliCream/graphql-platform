import React, { FC } from "react";
import { Tabs } from "./tabs";

export interface ApiChoiceTabsComposition {
  MinimalApis: FC;
  Regular: FC;
}

export const ApiChoiceTabs: FC & ApiChoiceTabsComposition = ({ children }) => {
  return (
    <Tabs defaultValue={"minimal-apis"} groupId="api-choice">
      <Tabs.List>
        <Tabs.Tab value="minimal-apis">.NET 6</Tabs.Tab>
        <Tabs.Tab value="regular">.NET 5 or earlier</Tabs.Tab>
      </Tabs.List>
      {children}
    </Tabs>
  );
};

const MinimalApis: FC = ({ children }) => (
  <Tabs.Panel value="minimal-apis">{children}</Tabs.Panel>
);

const Regular: FC = ({ children }) => (
  <Tabs.Panel value="regular">{children}</Tabs.Panel>
);

ApiChoiceTabs.MinimalApis = MinimalApis;
ApiChoiceTabs.Regular = Regular;
