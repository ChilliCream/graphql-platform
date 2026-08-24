import type { ReactNode } from "react";

import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";

interface ChartTileProps {
  readonly title: string;
  readonly hint?: string;
  readonly disclosure?: string;
  readonly glow?: boolean;
  readonly children: ReactNode;
}

export function ChartTile({
  title,
  hint,
  disclosure,
  glow = false,
  children,
}: ChartTileProps) {
  return (
    <Card variant="tile" glow={glow}>
      <div className="flex items-baseline justify-between gap-3">
        <h3 className="text-cc-heading font-heading text-h6">{title}</h3>
        {hint && <Eyebrow size="2xs">{hint}</Eyebrow>}
      </div>
      {disclosure && (
        <p className="text-cc-ink-dim mt-2 font-mono text-[0.62rem] tracking-[0.12em] uppercase">
          {disclosure}
        </p>
      )}
      <div className="mt-4">{children}</div>
    </Card>
  );
}
