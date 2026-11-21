import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { RetryVisual } from "./RetryVisual";

const meta = {
  title: "Mocha/RetryVisual",
  component: RetryVisual,
  parameters: { layout: "fullscreen" },
  // Runs a timed animation loop, so visual snapshots would be
  // nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="w-[860px] max-w-full">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof RetryVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
