import type { CSSProperties, ReactNode } from "react";

import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { NitroTheme } from "@/src/nitro";

interface BentoCardProps {
  readonly title: string;
  readonly hint?: ReactNode;
  readonly className?: string;
  readonly bodyClassName?: string;
  readonly children: ReactNode;
}

export function BentoCard({
  title,
  hint,
  className,
  bodyClassName = "px-5 pt-3 pb-5",
  children,
}: BentoCardProps) {
  return (
    <Card className={className}>
      <div className="relative z-10 flex h-full flex-col">
        <div className="flex items-baseline justify-between gap-3 px-5 pt-5">
          <Eyebrow as="h4" color="ink-dim">
            {title}
          </Eyebrow>
          {hint && (
            <Eyebrow as="span" color="ink-dim">
              {hint}
            </Eyebrow>
          )}
        </div>
        <div className={bodyClassName}>{children}</div>
      </div>
    </Card>
  );
}

interface ChartFrameProps {
  readonly children: ReactNode;
  readonly className?: string;
  readonly style?: CSSProperties;
}

/** Forces the Nitro dark chart palette for a primitive, regardless of the page's own theme. */
export function ChartFrame({ children, className, style }: ChartFrameProps) {
  return (
    <NitroTheme
      theme="dark"
      className={className}
      style={{ background: "transparent", ...style }}
    >
      {children}
    </NitroTheme>
  );
}
