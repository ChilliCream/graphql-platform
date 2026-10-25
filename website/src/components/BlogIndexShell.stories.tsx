import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import type { BlogTeaserData } from "./BlogTeaser";
import { BlogIndexShell } from "./BlogIndexShell";

const FEATURED_IMAGE = "/placeholders/featured.png";
const AVATAR_IMAGE = "/placeholders/avatar.png";

const SAMPLE: BlogTeaserData[] = [
  {
    href: "/blog/2025/02/01/hot-chocolate-15",
    title: "What's new for Hot Chocolate 15",
    date: "2025-02-01",
    featuredImage: FEATURED_IMAGE,
    author: "Michael Staib",
    authorImageUrl: AVATAR_IMAGE,
  },
  {
    href: "/blog/2024/08/30/hot-chocolate-14",
    title: "A particularly long blog post title that wraps across lines",
    date: "2024-08-30",
    featuredImage: FEATURED_IMAGE,
    author: "Rafael Staib",
    authorImageUrl: AVATAR_IMAGE,
  },
  {
    href: "/blog/2024/08/11/logging",
    title: "Logging in Banana Cake Pop",
    date: "2024-08-11",
    featuredImage: FEATURED_IMAGE,
    author: "Pascal Senn",
    authorImageUrl: AVATAR_IMAGE,
  },
];

const meta = {
  title: "Components/BlogIndexShell",
  component: BlogIndexShell,
} satisfies Meta<typeof BlogIndexShell>;

export default meta;
type Story = StoryObj<typeof meta>;

export const PlainHeading: Story = {
  args: {
    title: "Blog",
    posts: SAMPLE,
    pagination: {
      currentPage: 1,
      totalPages: 3,
      hrefForPage: (p) => (p === 1 ? "/blog" : `/blog/${p}`),
    },
  },
};

export const EmptyStateNoPagination: Story = {
  args: {
    title: "Blog",
    posts: [],
    pagination: undefined,
  },
};

export const HeaderWithPostCount: Story = {
  args: {
    title: "#graphql",
    subtitle: (
      <p className="text-cc-ink-dim text-sm">3 posts tagged “graphql”.</p>
    ),
    posts: SAMPLE,
    pagination: {
      currentPage: 1,
      totalPages: 2,
      hrefForPage: (p) =>
        p === 1 ? "/blog/tags/graphql" : `/blog/tags/graphql/${p}`,
    },
  },
};
