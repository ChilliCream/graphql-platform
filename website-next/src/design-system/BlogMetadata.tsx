type BlogMetadataProps = {
  author?: string;
  authorUrl?: string;
  authorImageUrl?: string;
  date?: string;
};

export function BlogMetadata({
  author,
  authorUrl,
  authorImageUrl,
  date,
}: BlogMetadataProps) {
  if (!author && !date) {
    return null;
  }

  return (
    <div className="flex flex-row items-center text-sm text-slate-600">
      {author ? (
        <a
          href={authorUrl || "#"}
          className="flex items-center text-slate-700 hover:text-emerald-700 no-underline"
          target={authorUrl?.startsWith("http") ? "_blank" : undefined}
          rel={authorUrl?.startsWith("http") ? "noopener noreferrer" : undefined}
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
      {date ? <span>{author ? " ・ " : ""}{date}</span> : null}
    </div>
  );
}
