import type { CSSProperties, ReactNode } from "react";

import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { NitroTheme } from "@/src/nitro";

interface BentoCardProps {
  readonly title: string;
  readonly hint?: ReactNode;
  readonly className?: string;
  readonly children: ReactNode;
}

/** One card of the Insights bento, in the same chrome as the Nitro page's SignalsBento. */
export function BentoCard({
  title,
  hint,
  className,
  children,
}: BentoCardProps) {
  return (
    <Card className={className}>
      <div className="flex items-baseline justify-between gap-3 px-4 pt-4">
        <Eyebrow as="h4" color="ink-dim">
          {title}
        </Eyebrow>
        {hint && (
          <Eyebrow as="span" color="ink-dim">
            {hint}
          </Eyebrow>
        )}
      </div>
      <div className="px-4 pt-3 pb-4">{children}</div>
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
