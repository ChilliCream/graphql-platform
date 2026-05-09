import Link from "next/link";

const blogs: { href: string; title: string }[] = [
  {
    href: "/blogs/2026/05/09/linking-between-posts",
    title: "Linking between blog posts (relative .md links demo)",
  },
  { href: "/blogs/2025/02/01/hot-chocolate-15", title: "Hot Chocolate 15" },
  { href: "/blogs/2024/10/07/introducing-nitro", title: "Introducing Nitro" },
  { href: "/blogs/2024/08/30/hot-chocolate-14", title: "Hot Chocolate 14" },
  {
    href: "/blogs/2023/08/15/graphql-fusion",
    title: "Announcing GraphQL Fusion",
  },
  { href: "/blogs/2019/05/08/performance", title: "Performance Improvements" },
];

export default function Home() {
  return (
    <>
      <h2>Docs</h2>
      <ul>
        <li>
          <Link href="/docs/fusion">Fusion</Link>
        </li>
        <li>
          <Link href="/docs/hotchocolate">Hot Chocolate</Link>
        </li>
        <li>
          <Link href="/docs/mocha">Mocha</Link>
        </li>
        <li>
          <Link href="/docs/nitro">Nitro</Link>
        </li>
        <li>
          <Link href="/docs/strawberryshake">Strawberry Shake</Link>
        </li>
      </ul>

      <h2>Blog</h2>
      <ul>
        {blogs.map((blog) => (
          <li key={blog.href}>
            <Link href={blog.href}>{blog.title}</Link>
          </li>
        ))}
      </ul>
    </>
  );
}
