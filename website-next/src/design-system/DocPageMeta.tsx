type DocPageMetaProps = {
  isoDate: string;
  displayDate: string;
  author: string;
};

export function DocPageMeta({ isoDate, displayDate, author }: DocPageMetaProps) {
  return (
    <footer className="mt-12 text-right text-sm text-cc-ink-dim">
      Last updated on{" "}
      <time dateTime={isoDate}>
        <strong>{displayDate}</strong>
      </time>{" "}
      by <strong>{author}</strong>
    </footer>
  );
}
