import type { CSSProperties } from "react";
import { Eyebrow } from "@/src/design-system/Eyebrow";

interface StatStripProps {
  readonly items: readonly { readonly label: string; readonly value: string }[];
  readonly className?: string;
}

/**
 * A compact strip of label/value stats in equal-width cells divided by hairline
 * borders. Column count follows the number of items.
 */
export function StatStrip({ items, className = "" }: StatStripProps) {
  return (
    <dl
      className={`border-cc-card-border bg-cc-card-border mx-auto grid gap-px overflow-hidden rounded-2xl border ${className}`}
      style={
        {
          gridTemplateColumns: `repeat(${items.length}, minmax(0, 1fr))`,
        } as CSSProperties
      }
    >
      {items.map((item) => (
        <div
          key={item.label}
          className="bg-cc-surface px-4 py-5 text-center sm:px-6"
        >
          <Eyebrow as="dt" size="2xs">
            {item.label}
          </Eyebrow>
          <dd className="font-heading text-cc-heading mt-2 text-xl font-semibold sm:text-2xl">
            {item.value}
          </dd>
        </div>
      ))}
    </dl>
  );
}
