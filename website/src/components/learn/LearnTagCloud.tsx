import { Tag } from "@/src/design-system/Tag";

interface LearnTagCloudProps {
  readonly tags: readonly string[];
}

/** "Most popular" rail unit (learn-editorial.md section 14.4): a mono-caption heading over the site's most frequent article tags. */
export function LearnTagCloud({ tags }: LearnTagCloudProps) {
  if (tags.length === 0) {
    return null;
  }
  return (
    <div>
      <p className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">Most popular</p>
      <div className="mt-3 flex flex-wrap gap-2">
        {tags.map((tag) => (
          <Tag key={tag} href={`/learn/articles/tags/${tag}`}>
            {tag}
          </Tag>
        ))}
      </div>
    </div>
  );
}
