import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TopologyVisual } from "./TopologyVisual";

const meta = {
  title: "Pages/Mocha/Visuals/TopologyVisual",
  component: TopologyVisual,
  parameters: { layout: "fullscreen" },
  // Runs a timed loop tracing routes through the generated topology, so
  // visual snapshots would be nondeterministic.
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
} satisfies Meta<typeof TopologyVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
