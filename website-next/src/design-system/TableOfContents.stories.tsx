import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { TableOfContents } from "./TableOfContents";

const meta = {
  title: "Design System/TableOfContents",
  component: TableOfContents,
} satisfies Meta<typeof TableOfContents>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    items: [
      { id: "introduction", text: "Introduction", depth: 2 },
      { id: "getting-started", text: "Getting Started", depth: 2 },
      { id: "installation", text: "Installation", depth: 3 },
      { id: "configuration", text: "Configuration", depth: 3 },
      { id: "advanced-usage", text: "Advanced Usage", depth: 2 },
      { id: "examples", text: "Examples", depth: 3 },
    ],
  },
};

export const Empty: Story = {
  args: { items: [] },
};

export const Flat: Story = {
  args: {
    items: [
      { id: "overview", text: "Overview", depth: 2 },
      { id: "api", text: "API", depth: 2 },
      { id: "faq", text: "FAQ", depth: 2 },
    ],
  },
};
