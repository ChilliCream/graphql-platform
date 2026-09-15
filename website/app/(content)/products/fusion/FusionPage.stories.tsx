import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { FusionPage } from "./FusionPage";

const meta = {
  title: "Pages/Products/Fusion",
  component: FusionPage,
  parameters: { layout: "fullscreen" },
  tags: ["no-snapshot"],
} satisfies Meta<typeof FusionPage>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
