"use client";

import { Children, isValidElement, type FC, type ReactNode } from "react";
import { usePathname } from "next/navigation";
import { Tab, Tabs } from "@/src/design-system/Tabs";

type ExampleTabKind = "implementation" | "code" | "schema";

interface ExampleTabMarkerProps {
  readonly children?: ReactNode;
  readonly exampleTab: ExampleTabKind;
}

interface ExampleTabsProps {
  readonly children?: ReactNode;
}

export const Implementation: FC<ExampleTabMarkerProps> = () => null;
export const Code: FC<ExampleTabMarkerProps> = () => null;
export const Schema: FC<ExampleTabMarkerProps> = () => null;
export const ExampleCode = Code;

const LABELS = new Map<ExampleTabKind, string>([
  ["implementation", "Implementation-first"],
  ["code", "Code-first"],
  ["schema", "Schema-first"],
]);

const ORDER: ExampleTabKind[] = ["implementation", "code", "schema"];

function isExampleTabKind(value: unknown): value is ExampleTabKind {
  return LABELS.has(value as ExampleTabKind);
}

export const ExampleTabs: FC<ExampleTabsProps> = ({ children }) => {
  const pathname = usePathname();
  const showSchema = !pathname?.includes("/v16/");

  const byType = new Map<ExampleTabKind, ReactNode>();
  for (const child of Children.toArray(children)) {
    if (!isValidElement(child)) {
      continue;
    }
    const props = child.props as Partial<ExampleTabMarkerProps>;
    if (isExampleTabKind(props.exampleTab)) {
      byType.set(props.exampleTab, props.children);
    }
  }

  const tabs = ORDER.filter((type) => byType.has(type)).filter(
    (type) => showSchema || type !== "schema",
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
