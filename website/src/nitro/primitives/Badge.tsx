import type { CSSProperties, ReactNode } from "react";
import { token } from "../lib/tokens";

export type BadgeSize = "sm" | "xs";

interface BadgeProps {
  children?: ReactNode;
  icon?: ReactNode;
  letter?: string;
  square?: boolean;
  size?: BadgeSize;
  mono?: boolean;
  bold?: boolean;
  lowercase?: boolean;
  background?: string;
  border?: string | false;
  color?: string;
  className?: string;
  style?: CSSProperties;
}

const SIZE_FONT: Record<BadgeSize, number> = {
  sm: 11,
  xs: 9.5,
};

const SIZE_PADDING: Record<BadgeSize, string> = {
  sm: "2px 7px",
  xs: "1px 5px",
};

const SQUARE_DIM: Record<BadgeSize, number> = {
  sm: 18,
  xs: 17,
};

export function Badge({
  children,
  icon,
  letter,
  square = false,
  size = "sm",
  mono = false,
  bold = false,
  lowercase = false,
  background = "transparent",
  border = token.border,
  color = token.textSecondary,
  className,
  style,
}: BadgeProps) {
  if (square) {
    const dim = SQUARE_DIM[size];
    return (
      <span
        className={className}
        style={{
          flex: "0 0 auto",
          width: dim,
          height: dim,
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          borderRadius: 3,
          background,
          border: border === false ? undefined : `1px solid ${border}`,
          color,
          fontFamily: mono ? token.mono : undefined,
          fontSize: 10.5,
          fontWeight: 700,
          lineHeight: 1,
          ...style,
        }}
      >
        {icon ?? letter}
      </span>
    );
  }

  return (
    <span
      className={className}
      style={{
        flex: "0 0 auto",
        display: "inline-flex",
        alignItems: "center",
        gap: icon ? 4 : 0,
        fontSize: SIZE_FONT[size],
        fontWeight: bold ? 600 : 500,
        padding: SIZE_PADDING[size],
        borderRadius: 4,
        background,
        border: border === false ? undefined : `1px solid ${border}`,
        color,
        fontFamily: mono ? token.mono : undefined,
        textTransform: lowercase ? "lowercase" : undefined,
        whiteSpace: "nowrap",
        ...style,
      }}
    >
      {icon}
      {children}
    </span>
  );
}
