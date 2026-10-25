import { Children, isValidElement, type FC, type ReactNode } from "react";
import { Tab, Tabs } from "@/src/design-system/Tabs";
import { Admonition } from "@/src/design-system/Admonition";
import { Link } from "@/src/design-system/Link";

type WithChildren = { children?: ReactNode };

const GitHubAction: FC<WithChildren> = () => null;
const AzureDevOps: FC<WithChildren> = () => null;
const CLI: FC<WithChildren> = () => null;

const LABELS = new Map<unknown, string>([
  [GitHubAction, "GitHub Actions"],
  [AzureDevOps, "Azure DevOps"],
  [CLI, "CLI"],
]);

const ORDER = [GitHubAction, AzureDevOps, CLI];

// Azure DevOps pipelines need the Nitro tasks extension installed before any
// task resolves, plus a configured authentication method. Surface this once,
// fixed at the top of the Azure DevOps tab, so every usage carries the setup
// note without repeating it in the docs.
const AzureDevOpsNote: FC = () => (
  <Admonition kind="note">
    <p>
      Install the{" "}
      <Link href="https://marketplace.visualstudio.com/items?itemName=ChilliCream.nitro-azure-pipelines-tasks">
        Nitro Azure Pipelines Tasks
      </Link>{" "}
      extension into your Azure DevOps organization before using these tasks,
      then configure authentication as described in the{" "}
      <Link href="https://github.com/ChilliCream/nitro-azure-pipelines-tasks/tree/main#authentication">
        authentication guide
      </Link>
      .
    </p>
  </Admonition>
);

type PipelineChoiceTabsType = FC<WithChildren> & {
  GitHubAction: FC<WithChildren>;
  AzureDevOps: FC<WithChildren>;
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
          {type === AzureDevOps && <AzureDevOpsNote />}
          {byType.get(type)}
        </Tab>
      ))}
    </Tabs>
  );
};

PipelineChoiceTabs.GitHubAction = GitHubAction;
PipelineChoiceTabs.AzureDevOps = AzureDevOps;
PipelineChoiceTabs.CLI = CLI;
