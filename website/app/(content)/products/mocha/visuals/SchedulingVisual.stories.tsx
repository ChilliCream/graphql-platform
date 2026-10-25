import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { SchedulingVisual } from "./SchedulingVisual";

const meta = {
  title: "Pages/Mocha/Visuals/SchedulingVisual",
  component: SchedulingVisual,
  parameters: { layout: "fullscreen" },
  // Runs a timed scheduling loop (message parked, waiting, then delivered
  // at its due time), so visual snapshots would be nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="w-[680px] max-w-full">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof SchedulingVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
