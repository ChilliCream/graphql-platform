type BlogMetadataProps = {
  author?: string;
  authorUrl?: string;
  authorImageUrl?: string;
  date?: string;
  readingTime?: string;
};

export function BlogMetadata({
  author,
  authorUrl,
  authorImageUrl,
  date,
  readingTime,
}: BlogMetadataProps) {
  if (!author && !date && !readingTime) {
    return null;
  }

  const parts = [
    date ? <span key="date">{date}</span> : null,
    readingTime ? <span key="rt">{readingTime}</span> : null,
  ].filter(Boolean);

  // TODO: Fix image and custom link
  return (
    <div className="flex flex-row items-center gap-2 text-sm text-cc-ink-dim">
      {author ? (
        <a
          href={authorUrl || "#"}
          className="flex items-center text-cc-ink hover:text-cc-accent no-underline"
          target={authorUrl?.startsWith("http") ? "_blank" : undefined}
          rel={
            authorUrl?.startsWith("http") ? "noopener noreferrer" : undefined
          }
        >
          {authorImageUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={authorImageUrl}
              alt={`${author}'s avatar`}
              width={30}
              height={30}
              loading="lazy"
              decoding="async"
              className="mr-2 h-[30px] w-[30px] rounded-full object-cover"
            />
          ) : null}
          <span>{author}</span>
        </a>
      ) : null}
      {parts.map((part, i) => (
        <span key={i} className="flex items-center gap-2">
          {author || i > 0 ? <span aria-hidden="true">·</span> : null}
          {part}
        </span>
      ))}
    </div>
  );
}
