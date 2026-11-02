import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TraceBento } from "./TraceBento";

const meta = {
  title: "Pages/Mocha/Visuals/TraceBento",
  component: TraceBento,
  parameters: { layout: "fullscreen" },
  // Embeds Nitro's TraceWaterfall/CountUp/Sparkline primitives, which
  // self-animate on their own timed clock, so visual snapshots would be
  // nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="w-[960px] max-w-full">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof TraceBento>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
