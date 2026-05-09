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
            className="block rounded-md border border-slate-200 bg-slate-50 px-3 py-1 text-sm text-slate-700 no-underline hover:border-slate-300 hover:bg-slate-100"
          >
            {tag}
          </Link>
        </li>
      ))}
    </ul>
  );
}
