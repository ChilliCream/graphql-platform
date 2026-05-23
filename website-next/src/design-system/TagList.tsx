import { Tag } from "./Tag";

export type TagListItem = {
  label: string;
  href?: string;
};

type TagListProps = {
  tags: (TagListItem | string)[];
  /**
   * Build an href from a bare string tag. Ignored when items already carry
   * their own `href`. Useful when callers just pass a `string[]` and want
   * every tag linked to the same section.
   */
  hrefForTag?: (tag: string) => string;
  className?: string;
};

export function TagList({ tags, hrefForTag, className }: TagListProps) {
  const items = tags
    .map((entry): TagListItem | null => {
      if (typeof entry === "string") {
        if (!entry) {
          return null;
        }
        return { label: entry, href: hrefForTag?.(entry) };
      }
      if (!entry.label) {
        return null;
      }
      return { label: entry.label, href: entry.href ?? hrefForTag?.(entry.label) };
    })
    .filter((i): i is TagListItem => i !== null);

  if (items.length === 0) {
    return null;
  }

  return (
    <ul
      className={[
        "m-0 flex list-none flex-wrap gap-1.5 p-0",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      {items.map((item) => (
        <li key={item.label} className="m-0">
          {item.href ? (
            <Tag href={item.href}>{item.label}</Tag>
          ) : (
            <Tag>{item.label}</Tag>
          )}
        </li>
      ))}
    </ul>
  );
}
