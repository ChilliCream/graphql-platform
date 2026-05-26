"use client";

import {
  Children,
  isValidElement,
  type FC,
  type ReactNode,
} from "react";
import { usePathname } from "next/navigation";
import { Tab, Tabs } from "./Tabs";

type WithChildren = { children?: ReactNode };

export const Implementation: FC<WithChildren> = () => null;
export const Code: FC<WithChildren> = () => null;
export const Schema: FC<WithChildren> = () => null;
export const ExampleCode = Code;

const LABELS = new Map<unknown, string>([
  [Implementation, "Implementation-first"],
  [Code, "Code-first"],
  [Schema, "Schema-first"],
]);

const ORDER = [Implementation, Code, Schema];

export const ExampleTabs: FC<WithChildren> = ({ children }) => {
  const pathname = usePathname();
  const showSchema = !pathname?.includes("/v16/");

  const byType = new Map<unknown, ReactNode>();
  for (const child of Children.toArray(children)) {
    if (!isValidElement(child)) {
      continue;
    }
    if (LABELS.has(child.type)) {
      byType.set(child.type, (child.props as WithChildren).children);
    }
  }

  const tabs = ORDER.filter((type) => byType.has(type)).filter(
    (type) => showSchema || type !== Schema
  );

  return (
    <Tabs>
      {tabs.map((type) => (
        <Tab key={LABELS.get(type)!} label={LABELS.get(type)!}>
          {byType.get(type)}
        </Tab>
      ))}
    </Tabs>
  );
};
