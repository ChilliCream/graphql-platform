import React, { FC } from "react";

import { List, Panel, Tab, Tabs } from "./tabs";

export const ExampleTabs: FC = ({ children }) => {
  return (
    <Tabs defaultValue={"implementation"} groupId="code-style">
      <List>
        <Tab value="implementation">Implementation-first</Tab>
        <Tab value="code">Code-first</Tab>
        <Tab value="schema">Schema-first</Tab>
      </List>
      {children}
    </Tabs>
  );
};

export const Implementation: FC = ({ children }) => (
  <Panel value="implementation">{children}</Panel>
);

export const Code: FC = ({ children }) => (
  <Panel value="code">{children}</Panel>
);

export const Schema: FC = ({ children }) => (
  <Panel value="schema">{children}</Panel>
);
