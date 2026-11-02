import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { TagList } from "./TagList";

const meta = {
  title: "Design System/TagList",
  component: TagList,
} satisfies Meta<typeof TagList>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Linked: Story = {
  args: {
    tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"],
    hrefForTag: (tag) => `/blog/tags/${tag}`,
  },
};

export const Static: Story = {
  args: {
    tags: ["v15", "deprecated", "beta"],
  },
};

export const Mixed: Story = {
  args: {
    tags: [
      { label: "hotchocolate", href: "/blog/tags/hotchocolate" },
      { label: "v15" },
      { label: "graphql", href: "/blog/tags/graphql" },
    ],
  },
};
