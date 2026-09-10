import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { QuickstartVisual } from "./QuickstartVisual";

const meta = {
  title: "Pages/Mocha/Visuals/QuickstartVisual",
  component: QuickstartVisual,
  parameters: { layout: "fullscreen" },
  // Spotlights each quickstart step in a timed loop with a travelling pulse
  // along the rail, so visual snapshots would be nondeterministic.
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
} satisfies Meta<typeof QuickstartVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
