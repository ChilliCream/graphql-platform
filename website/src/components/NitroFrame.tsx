import type { CSSProperties, ReactNode } from "react";

import { NitroTheme } from "@/src/nitro";

interface NitroFrameProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly style?: CSSProperties;
  readonly reducedMotion?: "user" | "always" | "never";
}

export function NitroFrame({ children, className, style, reducedMotion = "never" }: NitroFrameProps) {
  return (
    <NitroTheme
      theme="dark"
      reducedMotion={reducedMotion}
      className={className}
      style={{ background: "transparent", ...style }}
    >
      {children}
    </NitroTheme>
  );
}
