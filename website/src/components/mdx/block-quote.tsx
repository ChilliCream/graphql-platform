import React, { Children, FC } from "react";

import { Warning } from "./warning";

const warningText = "Warning: ";

interface PropsWithChildren {
  children?: React.ReactNode;
}

function getChildren(
  element: React.ReactElement
): React.ReactNode | undefined {
  return (element.props as PropsWithChildren).children;
}

export const BlockQuote: FC<{ children: React.ReactNode }> = ({ children }) => {
  const childArray = Children.toArray(children);

  const firstChild = childArray[0];

  let isWarning = false;

  if (React.isValidElement(firstChild)) {
    const firstChildren = getChildren(firstChild);

    if (typeof firstChildren === "string") {
      isWarning = firstChildren.startsWith(warningText);
    } else if (Array.isArray(firstChildren) && firstChildren.length > 0) {
      const first = firstChildren[0];
      isWarning =
        typeof first === "string" && first.startsWith(warningText);
    }
  }

  if (isWarning) {
    const elements = childArray.filter((child) =>
      React.isValidElement(child)
    ) as React.ReactElement[];
    const texts = elements.map((elem) => {
      const innerChildren = getChildren(elem);

      if (
        typeof innerChildren === "string" &&
        innerChildren.startsWith(warningText)
      ) {
        return innerChildren.substring(warningText.length);
      }

      if (
        Array.isArray(innerChildren) &&
        typeof innerChildren[0] === "string"
      ) {
        return innerChildren.map((child) => {
          if (typeof child === "string" && child.startsWith(warningText)) {
            return child.substring(warningText.length);
          }

          return child;
        });
      }

      return innerChildren;
    });

    return (
      <Warning>
        {texts.map((text, index) => (
          <p key={index}>{text}</p>
        ))}
      </Warning>
    );
  }

  return <blockquote>{children}</blockquote>;
};
