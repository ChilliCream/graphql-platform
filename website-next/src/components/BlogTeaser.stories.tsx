import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { BlogTeaser } from "./BlogTeaser";

const meta = {
  title: "Components/BlogTeaser",
  component: BlogTeaser,
  decorators: [
    (Story) => (
      <div style={{ width: 360 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof BlogTeaser>;

export default meta;
type Story = StoryObj<typeof meta>;

export const WithImage: Story = {
  args: {
    post: {
      href: "/blog/2025/02/01/hot-chocolate-15",
      title: "What's new for Hot Chocolate 15",
      date: "2025-02-01",
      featuredImage: "https://picsum.photos/seed/hot-chocolate-15/800/450",
      author: "Michael Staib",
      authorImageUrl: "https://i.pravatar.cc/100?u=michael",
    },
  },
};

export const BrokenImage: Story = {
  args: {
    post: {
      href: "/blog/2024/01/01/example",
      title: "A post whose featured image fails to load",
      date: "2024-01-01",
      featuredImage: "https://example.com/does-not-exist.jpg",
      author: "Jane Doe",
      authorImageUrl: null,
    },
  },
};
