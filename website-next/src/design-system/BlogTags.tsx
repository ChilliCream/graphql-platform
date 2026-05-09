import Link from "next/link";

type BlogTagsProps = {
  tags?: string[];
};

export function BlogTags({ tags }: BlogTagsProps) {
  const visible = (tags ?? []).filter(
    (tag): tag is string => typeof tag === "string" && tag.length > 0
  );
  if (visible.length === 0) {
    return null;
  }

  return (
    <ul className="my-6 list-none p-0 flex flex-wrap gap-1.5">
      {visible.map((tag) => (
        <li key={tag} className="inline-block">
          <Link
            href={`/blogs/tags/${tag}`}
            className="block rounded-md border border-stone-300 px-3 py-1 text-sm text-stone-800 no-underline bg-gradient-to-br from-cyan-500/20 via-sky-500/20 to-blue-700/20 hover:border-stone-400"
          >
            {tag}
          </Link>
        </li>
      ))}
    </ul>
  );
}
