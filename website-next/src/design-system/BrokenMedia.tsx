// TODO: Create a better illustration

type BrokenMediaProps = {
  /** Short reason shown under the icon, e.g. "This image couldn't be loaded." */
  message: string;
};

/**
 * Placeholder shown when a media source (video, image) can't be resolved or
 * loaded: a broken-link illustration with a short message, sized like a 16:9
 * media frame.
 */
export function BrokenMedia({ message }: BrokenMediaProps) {
  return (
    // Rendered as <span>s (not <div>/<p>) so it stays valid phrasing content:
    // markdown wraps a standalone image in a <p>, and a block-level fallback
    // there would be invalid HTML and trigger a hydration error.
    <span className="my-6 flex aspect-video w-full flex-col items-center justify-center gap-3 rounded-md bg-cc-card-bg text-cc-ink-dim ring-1 ring-cc-card-border">
      <svg
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
        className="h-10 w-10"
        aria-hidden="true"
      >
        <path d="m18.84 12.25 1.72-1.71a5 5 0 0 0-7.07-7.07l-1.72 1.71" />
        <path d="m5.17 11.75-1.71 1.71a5 5 0 0 0 7.07 7.07l1.71-1.71" />
        <line x1="8" y1="2" x2="8" y2="5" />
        <line x1="2" y1="8" x2="5" y2="8" />
        <line x1="16" y1="19" x2="16" y2="22" />
        <line x1="19" y1="16" x2="22" y2="16" />
      </svg>
      <span className="text-sm font-medium">{message}</span>
    </span>
  );
}
