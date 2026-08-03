import type { ReactNode } from "react";

type KeyValueChipDensity = "compact" | "cozy";

interface KeyValueChipProps {
  readonly label: string;
  readonly value: ReactNode;
  /** Wraps the value in a `<code>` tag (default) or a plain `<span>`. */
  readonly valueAs?: "code" | "span";
  /** Row order: the label first (default), or the value first. */
  readonly order?: "label-first" | "value-first";
  /** Row spacing: `between` pushes label and value to opposite ends (default);
   * `start` keeps them together, reading left to right. */
  readonly justify?: "between" | "start";
  /** Row padding and gap: `compact` (default, gap-2 py-2) or `cozy`
   * (gap-3 py-2.5). */
  readonly density?: KeyValueChipDensity;
  /** Letter spacing on the label: `wide` (default, tracking-[0.12em]) or
   * `normal` (tracking-[0.08em]). */
  readonly labelTracking?: "wide" | "normal";
  /** Truncates the label at `min-w-0` instead of sizing to content (default
   * off). Ignored when `labelWidth` is set, which always shrinks and does
   * not truncate. */
  readonly labelTruncate?: boolean;
  /** Reserves a fixed label width instead of sizing to content. */
  readonly labelWidth?: string;
  readonly className?: string;
}

const DENSITY_CLASSES: Record<KeyValueChipDensity, string> = {
  compact: "gap-2 py-2",
  cozy: "gap-3 py-2.5",
};

const LABEL_TRACKING_CLASSES: Record<"wide" | "normal", string> = {
  wide: "tracking-[0.12em]",
  normal: "tracking-[0.08em]",
};

/**
 * A bordered label/value row: a small uppercase mono label paired with a
 * code-styled or plain value. Used for compact rows of key facts, e.g. a
 * pattern attribute, a feedback exchange, or an MCP tool and its hint.
 */
export function KeyValueChip({
  label,
  value,
  valueAs = "code",
  order = "label-first",
  justify = "between",
  density = "compact",
  labelTracking = "wide",
  labelTruncate = false,
  labelWidth,
  className = "",
}: KeyValueChipProps) {
  const labelSizingClass = labelWidth
    ? "shrink-0"
    : labelTruncate
      ? "min-w-0 truncate"
      : "";

  const labelNode = (
    <span
      className={`text-cc-nav-label font-mono text-[0.55rem] ${LABEL_TRACKING_CLASSES[labelTracking]} uppercase ${labelSizingClass}`.trim()}
      style={labelWidth ? { width: labelWidth } : undefined}
    >
      {label}
    </span>
  );

  const ValueTag = valueAs === "code" ? "code" : "span";
  const valueNode = (
    <ValueTag
      className={
        valueAs === "code"
          ? "text-cc-accent shrink-0 font-mono text-[0.65rem]"
          : "text-cc-ink min-w-0 font-mono text-xs"
      }
    >
      {value}
    </ValueTag>
  );

  return (
    <div
      className={`border-cc-card-border bg-cc-surface flex items-center rounded-lg border px-3 ${DENSITY_CLASSES[density]} ${
        justify === "between" ? "justify-between" : ""
      } ${className}`.trim()}
    >
      {order === "label-first" ? (
        <>
          {labelNode}
          {valueNode}
        </>
      ) : (
        <>
          {valueNode}
          {labelNode}
        </>
      )}
    </div>
  );
}
