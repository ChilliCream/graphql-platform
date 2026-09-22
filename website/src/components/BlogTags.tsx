import { TagList } from "@/src/design-system/TagList";

interface BlogTagsProps {
  readonly tags?: string[];
  /**
   * Builds the link target of a tag. Defaults to the blog tag pages; pass
   * `null` for a section that has no tag pages, which renders the tags as
   * plain, link-less chips.
   */
  readonly hrefForTag?: ((tag: string) => string) | null;
}

export function BlogTags({
  tags,
  hrefForTag = (tag) => `/blog/tags/${tag}`,
}: BlogTagsProps) {
  const visible = (tags ?? []).filter(
    (tag): tag is string => typeof tag === "string" && tag.length > 0,
  );
  if (visible.length === 0) {
    return null;
  }

  return (
    <TagList
      className="my-6"
      tags={visible}
      hrefForTag={hrefForTag ?? undefined}
    />
  );
}
