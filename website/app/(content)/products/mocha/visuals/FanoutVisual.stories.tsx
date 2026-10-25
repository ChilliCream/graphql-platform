import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { FanoutVisual } from "./FanoutVisual";

const meta = {
  title: "Pages/Mocha/Visuals/FanoutVisual",
  component: FanoutVisual,
  parameters: { layout: "fullscreen" },
  // Runs a timed publish/broadcast loop across subscriber queues, so visual
  // snapshots would be nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="w-[640px] max-w-full">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof FanoutVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
