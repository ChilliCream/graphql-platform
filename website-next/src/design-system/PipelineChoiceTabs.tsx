import {
  Children,
  isValidElement,
  type FC,
  type ReactNode,
} from "react";
import { Tab, Tabs } from "./Tabs";

type WithChildren = { children?: ReactNode };

const GitHubAction: FC<WithChildren> = () => null;
const CLI: FC<WithChildren> = () => null;

const LABELS = new Map<unknown, string>([
  [GitHubAction, "GitHub Action"],
  [CLI, "CLI"],
]);

const ORDER = [GitHubAction, CLI];

type PipelineChoiceTabsType = FC<WithChildren> & {
  GitHubAction: FC<WithChildren>;
  CLI: FC<WithChildren>;
};

export const PipelineChoiceTabs: PipelineChoiceTabsType = ({ children }) => {
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

PipelineChoiceTabs.GitHubAction = GitHubAction;
PipelineChoiceTabs.CLI = CLI;
