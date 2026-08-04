import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import type { BlogTeaserData } from "./BlogTeaser";
import { BlogTeaserGrid } from "./BlogTeaserGrid";

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
    title:
      "A particularly long blog post title that wraps across multiple lines so equal-height teasers can be verified",
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
  {
    href: "/blog/2024/04/01/fullstack-workshop",
    title: "Fullstack workshop",
    date: "2024-04-01",
    featuredImage: FEATURED_IMAGE,
    author: "Michael Staib",
    authorImageUrl: AVATAR_IMAGE,
  },
  {
    href: "/blog/2023/08/15/fusion",
    title: "Fusion: an open approach to distributed GraphQL",
    date: "2023-08-15",
    featuredImage: FEATURED_IMAGE,
    author: "Michael Staib",
    authorImageUrl: AVATAR_IMAGE,
  },
  {
    href: "/blog/2023/02/07/new-in-banana-cake-pop-4",
    title: "New in Banana Cake Pop 4",
    date: "2023-02-07",
    featuredImage: FEATURED_IMAGE,
    author: "Rafael Staib",
    authorImageUrl: AVATAR_IMAGE,
  },
];

const meta = {
  title: "Components/BlogTeaserGrid",
  component: BlogTeaserGrid,
  parameters: { layout: "centered" },
  decorators: [
    (Story) => (
      <div style={{ width: "min(100%, 1100px)" }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof BlogTeaserGrid>;

export default meta;
type Story = StoryObj<typeof meta>;

export const PartialRow: Story = {
  args: { posts: SAMPLE.slice(0, 4) },
};
