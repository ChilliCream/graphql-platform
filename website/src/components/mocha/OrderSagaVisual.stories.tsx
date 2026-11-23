import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { OrderSagaVisual } from "./OrderSagaVisual";

const meta = {
  title: "Mocha/OrderSagaVisual",
  component: OrderSagaVisual,
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
} satisfies Meta<typeof OrderSagaVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

/** The phone layout: the states stack top to bottom. */
export const Narrow: Story = {
  decorators: [
    (Story) => (
      <div className="w-[350px]">
        <Story />
      </div>
    ),
  ],
};
