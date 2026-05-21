import Link from "next/link";
import { Typography } from "@/src/design-system/Typography";
import { listBlogPosts } from "@/src/helpers/blogPaths";

export const metadata = {
  title: "Blog",
  description: "The ChilliCream blog: announcements, deep dives, and how-tos.",
};

export default function BlogsIndex() {
  const posts = listBlogPosts().filter(
    ({ parsed }) => parsed.slug !== "__empty__",
  );

  return (
    <div className="px-5 py-8 sm:px-12">
      <div className="mx-auto max-w-5xl">
        <Typography variant="h1">Blog</Typography>
        {posts.length === 0 ? (
          <p className="text-stone-600">
            New posts are on their way. Check back soon.
          </p>
        ) : (
          <ul className="mt-6 flex flex-col gap-3">
            {posts.map(({ parsed }) => {
              const href = `/blogs/${parsed.year}/${parsed.month}/${parsed.day}/${parsed.slug}`;
              return (
                <li key={href}>
                  <Link
                    href={href}
                    className="block rounded-md border border-stone-200 p-4 text-stone-800 no-underline transition-colors hover:border-fuchsia-700 hover:text-fuchsia-700"
                  >
                    <div className="font-medium">{parsed.slug}</div>
                    <div className="text-sm text-stone-500">
                      {parsed.year}-{parsed.month}-{parsed.day}
                    </div>
                  </Link>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </div>
  );
}
