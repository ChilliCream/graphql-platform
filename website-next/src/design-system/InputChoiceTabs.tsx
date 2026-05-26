import {
  Children,
  isValidElement,
  type FC,
  type ReactNode,
} from "react";
import { Tab, Tabs } from "./Tabs";

type WithChildren = { children?: ReactNode };

const CLI: FC<WithChildren> = () => null;
const VisualStudio: FC<WithChildren> = () => null;

const LABELS = new Map<unknown, string>([
  [CLI, "CLI"],
  [VisualStudio, "Visual Studio"],
]);

const ORDER = [CLI, VisualStudio];

type InputChoiceTabsType = FC<WithChildren> & {
  CLI: FC<WithChildren>;
  VisualStudio: FC<WithChildren>;
};

export const InputChoiceTabs: InputChoiceTabsType = ({ children }) => {
  const byType = new Map<unknown, ReactNode>();
  for (const child of Children.toArray(children)) {
    if (!isValidElement(child)) {
      continue;
    }
    if (LABELS.has(child.type)) {
      byType.set(child.type, (child.props as WithChildren).children);
    }
  }

  return (
    <Tabs>
      {ORDER.filter((type) => byType.has(type)).map((type) => (
        <Tab key={LABELS.get(type)!} label={LABELS.get(type)!}>
          {byType.get(type)}
        </Tab>
      ))}
    </Tabs>
  );
};

InputChoiceTabs.CLI = CLI;
InputChoiceTabs.VisualStudio = VisualStudio;
