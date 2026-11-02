import type { CSSProperties, ElementType } from "react";
import { token } from "../lib/tokens";

export interface UnderlineTabProps {
  label: string;
  active: boolean;
  as?: ElementType;
  color?: string;
  fontSize?: number;
  fontWeight?: CSSProperties["fontWeight"];
  activeFontWeight?: CSSProperties["fontWeight"];
  underlineOffset?: number;
  underlineInset?: number;
  height?: CSSProperties["height"];
  testId?: string;
  className?: string;
  style?: CSSProperties;
}

export function UnderlineTab({
  label,
  active,
  as: As = "span",
  color = token.graphEdgeActive,
  fontSize = 13,
  fontWeight,
  activeFontWeight,
  underlineOffset = 0,
  underlineInset = 0,
  height,
  testId,
  className,
  style,
}: UnderlineTabProps) {
  const resolvedWeight = active ? (activeFontWeight ?? fontWeight) : fontWeight;

  return (
    <As
      data-testid={testId}
      className={className}
      style={{
        position: "relative",
        display: "flex",
        alignItems: "center",
        height,
        fontSize,
        fontWeight: resolvedWeight,
        color: active ? token.textStrong : token.textSecondary,
        ...style,
      }}
    >
      {label}
      {active && (
        <span
          style={{
            position: "absolute",
            left: underlineInset,
            right: underlineInset,
            bottom: underlineOffset,
            height: 2,
            background: color,
          }}
        />
      )}
    </As>
  );
}
