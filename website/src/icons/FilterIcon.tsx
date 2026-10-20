interface FilterIconProps {
  readonly className?: string;
}

export function FilterIcon({ className }: FilterIconProps) {
  return (
    <svg
      aria-hidden="true"
      className={className}
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M4 5h16l-6 7v6l-4 2v-8L4 5Z" />
    </svg>
  );
}
