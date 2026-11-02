import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { SendVisual } from "./SendVisual";

const meta = {
  title: "Pages/Mocha/Visuals/SendVisual",
  component: SendVisual,
  parameters: { layout: "fullscreen" },
  // Runs a timed send loop (command parked in the queue, handler picks it
  // up), so visual snapshots would be nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="w-[720px] max-w-full">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof SendVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
