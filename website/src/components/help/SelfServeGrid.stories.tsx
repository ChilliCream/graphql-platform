import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { SelfServeGrid } from "./SelfServeGrid";

const meta = {
  title: "Pages/Help/SelfServeGrid",
  component: SelfServeGrid,
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-6xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof SelfServeGrid>;

export default meta;
type Story = StoryObj<typeof meta>;

// Five linked self-serve channels: docs, blog, Slack, YouTube, GitHub.
export const Default: Story = {};
