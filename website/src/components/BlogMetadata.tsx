import Link from "next/link";
import { Picture } from "@/src/design-system/Picture";

interface BlogMetadataProps {
  readonly author?: string;
  readonly authorUrl?: string;
  readonly authorProfileHref?: string;
  readonly authorImageUrl?: string;
  readonly date?: string;
  readonly readingTime?: string;
}

export function BlogMetadata({
  author,
  authorUrl,
  authorProfileHref,
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

  const authorHref = authorProfileHref ?? authorUrl;
  const isExternalAuthorHref = authorHref?.startsWith("http") ?? false;
  const authorContent = (
    <>
      {authorImageUrl ? (
        <Picture
          src={authorImageUrl}
          alt={`${author}'s avatar`}
          width={30}
          height={30}
          sizes="30px"
          className="mr-2 h-[30px] w-[30px] rounded-full object-cover"
        />
      ) : null}
      <span>{author}</span>
    </>
  );

  return (
    <div className="text-cc-ink-dim flex flex-row items-center gap-2 text-sm">
      {author ? (
        authorHref ? (
          isExternalAuthorHref ? (
            <a
              href={authorHref}
              className="text-cc-ink-dim hover:text-cc-accent flex items-center no-underline"
              target="_blank"
              rel="noopener noreferrer"
            >
              {authorContent}
            </a>
          ) : (
            <Link
              href={authorHref}
              className="text-cc-ink-dim hover:text-cc-accent flex items-center no-underline"
            >
              {authorContent}
            </Link>
          )
        ) : (
          <span className="flex items-center">{authorContent}</span>
        )
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
