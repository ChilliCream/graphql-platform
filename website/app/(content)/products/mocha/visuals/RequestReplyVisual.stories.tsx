import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { RequestReplyVisual } from "./RequestReplyVisual";

const meta = {
  title: "Pages/Mocha/Visuals/RequestReplyVisual",
  component: RequestReplyVisual,
  parameters: { layout: "fullscreen" },
  // Runs a timed request/reply loop between caller and handler, so visual
  // snapshots would be nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="w-[560px] max-w-full">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof RequestReplyVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
