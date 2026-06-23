import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import type { BlogTeaserData } from "./BlogTeaser";
import { BlogTeaserGrid } from "./BlogTeaserGrid";

const img = (seed: string) => `https://picsum.photos/seed/${seed}/800/450`;
const avatar = (seed: string) =>
  `https://i.pravatar.cc/100?u=${encodeURIComponent(seed)}`;

const SAMPLE: BlogTeaserData[] = [
  {
    href: "/blog/2025/02/01/hot-chocolate-15",
    title: "What's new for Hot Chocolate 15",
    date: "2025-02-01",
    featuredImage: img("hot-chocolate-15"),
    author: "Michael Staib",
    authorImageUrl: avatar("michael"),
  },
  {
    href: "/blog/2024/08/30/hot-chocolate-14",
    title:
      "A particularly long blog post title that wraps across multiple lines so equal-height teasers can be verified",
    date: "2024-08-30",
    featuredImage: img("hot-chocolate-14"),
    author: "Rafael Staib",
    authorImageUrl: avatar("rafael"),
  },
  {
    href: "/blog/2024/08/11/logging",
    title: "Logging in Banana Cake Pop",
    date: "2024-08-11",
    featuredImage: img("logging"),
    author: "Pascal Senn",
    authorImageUrl: avatar("pascal"),
  },
  {
    href: "/blog/2024/04/01/fullstack-workshop",
    title: "Fullstack workshop",
    date: "2024-04-01",
    featuredImage: img("fullstack"),
    author: "Michael Staib",
    authorImageUrl: avatar("michael"),
  },
  {
    href: "/blog/2023/08/15/fusion",
    title: "Fusion: an open approach to distributed GraphQL",
    date: "2023-08-15",
    featuredImage: img("fusion"),
    author: "Michael Staib",
    authorImageUrl: avatar("michael"),
  },
  {
    href: "/blog/2023/02/07/new-in-banana-cake-pop-4",
    title: "New in Banana Cake Pop 4",
    date: "2023-02-07",
    featuredImage: img("bcp-4"),
    author: "Rafael Staib",
    authorImageUrl: avatar("rafael"),
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
