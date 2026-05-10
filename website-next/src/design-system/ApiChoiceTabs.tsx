import {
  Children,
  isValidElement,
  type FC,
  type ReactNode,
} from "react";
import { Tab, Tabs } from "./Tabs";

type WithChildren = { children?: ReactNode };

const MinimalApis: FC<WithChildren> = () => null;
const Regular: FC<WithChildren> = () => null;

const LABELS = new Map<unknown, string>([
  [MinimalApis, ".NET 6"],
  [Regular, ".NET 5 or earlier"],
]);

const ORDER = [MinimalApis, Regular];

type ApiChoiceTabsType = FC<WithChildren> & {
  MinimalApis: FC<WithChildren>;
  Regular: FC<WithChildren>;
};

export const ApiChoiceTabs: ApiChoiceTabsType = ({ children }) => {
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

ApiChoiceTabs.MinimalApis = MinimalApis;
ApiChoiceTabs.Regular = Regular;
