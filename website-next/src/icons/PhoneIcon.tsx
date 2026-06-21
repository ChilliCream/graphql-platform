import type { CSSProperties } from "react";

interface PhoneIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

export function PhoneIcon({ className, style }: PhoneIconProps) {
  return (
    <svg
      viewBox="474 6036 94 98"
      fill="none"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <path
        d="M506,6047c-2.21,0-4,1.79-4,4v48c0,2.21,1.79,4,4,4h28c2.21,0,4-1.79,4-4v-48c0-2.21-1.79-4-4-4h-28ZM498,6051c0-4.41,3.59-8,8-8h28c4.41,0,8,3.59,8,8v48c0,4.41-3.59,8-8,8h-28c-4.41,0-8-3.59-8-8v-48ZM516,6093h8c1.1,0,2,.9,2,2s-.9,2-2,2h-8c-1.1,0-2-.9-2-2s.9-2,2-2Z"
        fill="currentColor"
      />
    </svg>
  );
}
