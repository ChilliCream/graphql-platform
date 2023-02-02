import React, { Children, FC, ReactElement } from "react";

import { Warning } from "./warning";

const warningText = "Warning: ";

export const BlockQuote: FC = ({ children }) => {
  const childArray = Children.toArray(children);

  const isWarning =
    childArray.length > 0 &&
    React.isValidElement(childArray[0]) &&
    ((typeof childArray[0].props["children"] === "string" &&
      childArray[0].props["children"].startsWith(warningText)) ||
      (Array.isArray(childArray[0].props["children"]) &&
        childArray[0].props["children"].length > 0 &&
        typeof childArray[0].props["children"][0] === "string" &&
        childArray[0].props["children"][0].startsWith(warningText)));

  if (isWarning) {
    const elements = childArray.filter((child) =>
      React.isValidElement(child)
    ) as ReactElement[];
    const texts = elements.map((elem) => {
      const innerChildren = elem.props["children"];

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
