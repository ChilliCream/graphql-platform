interface CircleQuestionIconProps {
  readonly className?: string;
}

export function CircleQuestionIcon({ className }: CircleQuestionIconProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      width="14"
      height="14"
      viewBox="0 0 24 24"
    >
      <circle cx="12" cy="12" r="10" fill="currentColor" />
      <path
        d="M9.5 8.5a2.5 2.5 0 0 1 5 0c0 2-2.5 2-2.5 4"
        fill="none"
        stroke="var(--color-cc-bg)"
        strokeWidth="2"
        strokeLinecap="round"
      />
      <circle cx="12" cy="16.5" r="1.1" fill="var(--color-cc-bg)" />
    </svg>
  );
}
