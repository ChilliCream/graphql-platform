interface CrownIconProps {
  readonly className?: string;
}

export function CrownIcon({ className }: CrownIconProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="m3 6 5 4 4-6 4 6 5-4-3 12H6L3 6Z" />
      <path d="M7 21h10" />
    </svg>
  );
}
