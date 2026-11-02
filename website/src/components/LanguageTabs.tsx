"use client";

import { Children, isValidElement, type FC, type ReactNode } from "react";
import { Tab, Tabs } from "@/src/design-system/Tabs";

type LanguageTabKind = "csharp" | "typescript";

interface LanguageTabMarkerProps {
  readonly children?: ReactNode;
  readonly languageTab: LanguageTabKind;
}

interface LanguageTabsProps {
  readonly children?: ReactNode;
}

export const CSharp: FC<LanguageTabMarkerProps> = () => null;
export const TypeScript: FC<LanguageTabMarkerProps> = () => null;

const LABELS = new Map<LanguageTabKind, string>([
  ["csharp", "C#"],
  ["typescript", "TypeScript"],
]);

const ORDER: LanguageTabKind[] = ["csharp", "typescript"];

function isLanguageTabKind(value: unknown): value is LanguageTabKind {
  return LABELS.has(value as LanguageTabKind);
}

export const LanguageTabs: FC<LanguageTabsProps> = ({ children }) => {
  const byType = new Map<LanguageTabKind, ReactNode>();
  for (const child of Children.toArray(children)) {
    if (!isValidElement(child)) {
      continue;
    }
    const props = child.props as Partial<LanguageTabMarkerProps>;
    if (isLanguageTabKind(props.languageTab)) {
      byType.set(props.languageTab, props.children);
    }
  }

  const tabs = ORDER.filter((type) => byType.has(type));

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
