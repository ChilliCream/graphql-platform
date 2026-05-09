import type { ReactNode } from "react";
import { stepStyle } from "./languages";

type CodeStepProps = {
  step: number;
  children: ReactNode;
};

export function CodeStep({ step, children }: CodeStepProps) {
  return (
    <span
      className="rounded px-1 font-mono ring-1 inline-block leading-tight"
      style={stepStyle(step)}
    >
      {children}
    </span>
  );
}
