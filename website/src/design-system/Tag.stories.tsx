import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Tag } from "./Tag";

const meta = {
  title: "Design System/Tag",
  component: Tag,
} satisfies Meta<typeof Tag>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Link: Story = {
  args: {
    href: "/blog/tags/hotchocolate",
    children: "hotchocolate",
  },
};

export const Static: Story = {
  args: {
    children: "graphql",
  },
};
