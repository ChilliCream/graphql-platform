import {
  Children,
  cloneElement,
  isValidElement,
  type ReactElement,
  type ReactNode,
} from "react";
import type { AdmonitionKind } from "@/src/design-system/Admonition";

const ALERT_REGEX =
  /^\[!(NOTE|TIP|IMPORTANT|WARNING|CAUTION|EXPERIMENTAL)\]\s*\n?/;

export type AdmonitionMatch = {
  kind: AdmonitionKind;
  body: ReactNode;
};

export function detectAdmonition(children: ReactNode): AdmonitionMatch | null {
  const arr = Children.toArray(children);
  const firstIndex = arr.findIndex(
    (c) => isValidElement(c) || (typeof c === "string" && c.trim().length > 0)
  );
  if (firstIndex === -1) {
    return null;
  }

  const first = arr[firstIndex];
  if (!isValidElement(first)) {
    return null;
  }

  const innerChildren = Children.toArray(
    (first.props as { children?: ReactNode }).children
  );
  const firstInner = innerChildren[0];
  if (typeof firstInner !== "string") {
    return null;
  }

  const match = firstInner.match(ALERT_REGEX);
  if (!match) {
    return null;
  }

  const remaining = firstInner.slice(match[0].length);
  const newInner =
    remaining.length > 0
      ? [remaining, ...innerChildren.slice(1)]
      : innerChildren.slice(1);

  const newFirst = cloneElement(first as ReactElement, undefined, ...newInner);
  const rest = arr
    .slice(firstIndex + 1)
    .filter((c) => !(typeof c === "string" && c.trim().length === 0));

  return {
    kind: match[1].toLowerCase() as AdmonitionKind,
    body: [newFirst, ...rest],
  };
}
