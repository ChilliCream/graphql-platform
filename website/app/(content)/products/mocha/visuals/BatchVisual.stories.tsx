import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { BatchVisual } from "./BatchVisual";

const meta = {
  title: "Pages/Mocha/Visuals/BatchVisual",
  component: BatchVisual,
  parameters: { layout: "fullscreen" },
  // Runs a timed fill/flush loop (dots streaming in, compressing into a
  // batch, travelling to the handler), so visual snapshots would be
  // nondeterministic.
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
} satisfies Meta<typeof BatchVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
