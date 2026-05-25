import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Divider } from "./Divider";

const meta = {
  title: "Design System/Divider",
  component: Divider,
} satisfies Meta<typeof Divider>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const BetweenContent: Story = {
  render: () => (
    <div className="text-base text-stone-800">
      <p>Content above the divider.</p>
      <Divider />
      <p>Content below the divider.</p>
    </div>
  ),
};
