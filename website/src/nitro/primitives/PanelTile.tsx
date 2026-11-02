import type { CSSProperties, ReactNode } from "react";
import { token } from "../lib/tokens";

export interface PanelTileProps {
  title: string;
  subtitle?: ReactNode;
  headerExtra?: ReactNode;
  count?: string;
  borderStrong?: boolean;
  height?: number;
  flex?: CSSProperties["flex"];
  bodyPadding?: CSSProperties["padding"];
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
}

const HEADER_H = 36;

export function PanelTile({
  title,
  subtitle,
  headerExtra,
  count,
  borderStrong = false,
  height,
  flex = 1,
  bodyPadding = "12px 14px 10px",
  children,
  className,
  style,
}: PanelTileProps) {
  return (
    <div
      className={className}
      style={{
        flex,
        minWidth: 0,
        height,
        background: token.card,
        border: `1px solid ${borderStrong ? token.borderStrong : token.border}`,
        borderRadius: 8,
        display: "flex",
        flexDirection: "column",
        ...style,
      }}
    >
      <div
        style={{
          height: HEADER_H,
          flex: "0 0 auto",
          display: "flex",
          alignItems: "center",
          gap: 10,
          padding: "0 14px",
          borderBottom: `1px solid ${token.border}`,
        }}
      >
        <span
          style={{ fontSize: 13, fontWeight: 600, color: token.textStrong }}
        >
          {title}
        </span>
        {subtitle != null && (
          <span style={{ fontSize: 11.5, color: token.textSecondary }}>
            {subtitle}
          </span>
        )}
        {count != null && (
          <span
            style={{
              fontSize: 11.5,
              fontWeight: 600,
              color: token.textSecondary,
              background: token.surface,
              border: `1px solid ${token.border}`,
              borderRadius: 10,
              padding: "1px 8px",
            }}
          >
            {count}
          </span>
        )}
        {headerExtra != null && (
          <span style={{ marginLeft: "auto", display: "flex" }}>
            {headerExtra}
          </span>
        )}
      </div>
      <div
        style={{
          flex: 1,
          minHeight: 0,
          padding: bodyPadding,
          position: "relative",
        }}
      >
        {children}
      </div>
    </div>
  );
}
