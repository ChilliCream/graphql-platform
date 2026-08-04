import { TagList } from "@/src/design-system/TagList";

type BlogTagsProps = {
  tags?: string[];
};

export function BlogTags({ tags }: BlogTagsProps) {
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
      hrefForTag={(tag) => `/blog/tags/${tag}`}
    />
  );
}
