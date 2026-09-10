import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { MediatorVisual } from "./MediatorVisual";

const meta = {
  title: "Pages/Mocha/Visuals/MediatorVisual",
  component: MediatorVisual,
  parameters: { layout: "fullscreen" },
  // Runs a timed in-process dispatch loop through the mediator pipeline, so
  // visual snapshots would be nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="w-[1100px] max-w-full">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof MediatorVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
