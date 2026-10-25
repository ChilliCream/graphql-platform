import type { CSSProperties } from "react";

interface BrowserIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

export function BrowserIcon({ className, style }: BrowserIconProps) {
  return (
    <svg
      viewBox="153 6028 94 94"
      fill="none"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <path
        d="M224,6055c2.21,0,4,1.79,4,4v12h-56v-12c0-2.21,1.79-4,4-4h48ZM228,6075v16c0,2.21-1.79,4-4,4h-48c-2.21,0-4-1.79-4-4v-16h56ZM176,6051c-4.41,0-8,3.59-8,8v32c0,4.41,3.59,8,8,8h48c4.41,0,8-3.59,8-8v-32c0-4.41-3.59-8-8-8h-48ZM183,6063c0-1.66-1.34-3-3-3s-3,1.34-3,3,1.34,3,3,3,3-1.34,3-3ZM192,6066c1.66,0,3-1.34,3-3s-1.34-3-3-3-3,1.34-3,3,1.34,3,3,3ZM207,6063c0-1.66-1.34-3-3-3s-3,1.34-3,3,1.34,3,3,3,3-1.34,3-3Z"
        fill="currentColor"
      />
    </svg>
  );
}
