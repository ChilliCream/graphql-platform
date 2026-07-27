type DocPageMetaProps = {
  isoDate: string;
  displayDate: string;
  author: string;
};

export function DocPageMeta({
  isoDate,
  displayDate,
  author,
}: DocPageMetaProps) {
  return (
    <footer className="text-cc-ink-dim mt-12 text-right text-sm">
      Last updated on{" "}
      <time dateTime={isoDate}>
        <strong>{displayDate}</strong>
      </time>{" "}
      by <strong>{author}</strong>
    </footer>
  );
}
