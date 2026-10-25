import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { EngagementStrip } from "./EngagementStrip";

const meta = {
  title: "Pages/Advisory/EngagementStrip",
  component: EngagementStrip,
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
} satisfies Meta<typeof EngagementStrip>;

export default meta;
type Story = StoryObj<typeof meta>;

// The three engagement steps from first call to first commit.
export const Default: Story = {};
