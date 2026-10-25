import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { WhyMessagingVisual } from "./WhyMessagingVisual";

const meta = {
  title: "Pages/Mocha/Visuals/WhyMessagingVisual",
  component: WhyMessagingVisual,
  parameters: { layout: "fullscreen" },
  // Runs the timed send-vs-request/reply orientation loop, so visual
  // snapshots would be nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="w-[1100px] max-w-full">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof WhyMessagingVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
