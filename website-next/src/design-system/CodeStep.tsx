"use client";

import type { MouseEvent, ReactNode } from "react";
import { stepStyle } from "./languages";

type CodeStepProps = {
  step: number;
  children: ReactNode;
};

function findOwningFigure(el: Element): Element | null {
  let owner: Element | null = null;
  for (const fig of document.querySelectorAll("figure")) {
    if (el.compareDocumentPosition(fig) & Node.DOCUMENT_POSITION_PRECEDING) {
      owner = fig;
    }
  }
  return owner;
}

export function CodeStep({ step, children }: CodeStepProps) {
  function activate(e: MouseEvent<HTMLSpanElement>) {
    const owner = findOwningFigure(e.currentTarget);
    if (!owner) {
      return;
    }
    const matches = owner.querySelectorAll(`[data-step="${step}"]`);
    if (matches.length === 0) {
      return;
    }
    owner.classList.add("code-step-dim");
    for (const el of matches) {
      el.classList.add("code-step-active");
    }
  }

  function deactivate() {
    for (const el of document.querySelectorAll(".code-step-active")) {
      el.classList.remove("code-step-active");
    }
    for (const el of document.querySelectorAll(".code-step-dim")) {
      el.classList.remove("code-step-dim");
    }
  }

  return (
    <span
      className="code-step rounded px-1 font-mono ring-1 inline-block leading-tight"
      data-step={step}
      style={stepStyle(step)}
      onMouseEnter={activate}
      onMouseLeave={deactivate}
    >
      {children}
    </span>
  );
}
