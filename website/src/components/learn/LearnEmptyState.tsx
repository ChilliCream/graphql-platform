interface LearnEmptyStateProps {
  readonly heading: string;
  readonly description: string;
  readonly actionLabel: string;
  readonly onAction: () => void;
}

/** Dashed empty panel shown in place of the card grid when filters/search yield nothing, or a content type has no seeded items yet. */
export function LearnEmptyState({ heading, description, actionLabel, onAction }: LearnEmptyStateProps) {
  return (
    <div className="border-cc-card-border flex min-h-[24rem] flex-col items-center justify-center rounded-2xl border border-dashed px-8 py-20 text-center">
      <p className="text-cc-heading font-heading text-lg font-semibold">{heading}</p>
      <p className="text-cc-ink-dim mx-auto mt-2 max-w-md text-sm leading-relaxed">{description}</p>
      <button
        type="button"
        onClick={onAction}
        className="border-cc-card-border text-cc-heading hover:border-cc-accent hover:text-cc-accent mt-6 cursor-pointer rounded-full border px-5 py-2 text-sm font-medium transition-colors"
      >
        {actionLabel}
      </button>
    </div>
  );
}
