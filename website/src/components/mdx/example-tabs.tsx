"use client";

import React, { FC, useEffect } from "react";
import { usePathname } from "next/navigation";

import { List, Panel, Tab, Tabs, useTabs } from "./tabs";

/**
 * Resets the active tab to the default if the current tab is not in the
 * allowed set (e.g. "schema" tab hidden on v16 but stored in localStorage).
 */
const ResetDisallowedTab: FC<{ allowed: string[] }> = ({ allowed }) => {
  const { activeTab, setActiveTab } = useTabs();

  useEffect(() => {
    if (!allowed.includes(activeTab)) {
      setActiveTab(allowed[0]);
    }
  }, [activeTab, allowed, setActiveTab]);

  return null;
};

export const ExampleTabs: FC = ({ children }) => {
  const pathname = usePathname();
  const showSchema = !pathname?.includes("/v16/");

  return (
    <Tabs defaultValue={"implementation"} groupId="code-style">
      {!showSchema && (
        <ResetDisallowedTab allowed={["implementation", "code"]} />
      )}
      <List>
        <Tab value="implementation">Implementation-first</Tab>
        <Tab value="code">Code-first</Tab>
        {showSchema && <Tab value="schema">Schema-first</Tab>}
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

export const ExampleCode: FC = ({ children }) => (
  <Panel value="code">{children}</Panel>
);

export const Schema: FC = ({ children }) => {
  const pathname = usePathname();
  if (pathname?.includes("/v16/")) return null;
  return <Panel value="schema">{children}</Panel>;
};
