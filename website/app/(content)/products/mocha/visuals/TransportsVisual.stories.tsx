import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TransportsVisual } from "./TransportsVisual";

const meta = {
  title: "Pages/Mocha/Visuals/TransportsVisual",
  component: TransportsVisual,
  parameters: { layout: "fullscreen" },
  // Cycles a message through the different transport backends on a timed
  // loop, so visual snapshots would be nondeterministic.
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
} satisfies Meta<typeof TransportsVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
