import React, { Children, FC } from "react";
import { Warning } from "./warning";

const warningText = "Warning: ";

export const BlockQuote: FC = ({ children }) => {
  const child = Children.only(children);

  const texts = (child as any)?.props?.children;

  if (
    Array.isArray(texts) &&
    texts.length > 0 &&
    typeof texts[0] === "string"
  ) {
    if (texts[0].startsWith(warningText)) {
      const mutatedTexts = texts.slice(0);
      mutatedTexts[0] = texts[0].substring(warningText.length);

      return <Warning>{mutatedTexts}</Warning>;
    }
  }

  return <blockquote>{children}</blockquote>;
};
