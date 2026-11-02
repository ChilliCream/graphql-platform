import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { SagaVisual } from "./SagaVisual";

const meta = {
  title: "Pages/Mocha/Visuals/SagaVisual",
  component: SagaVisual,
  parameters: { layout: "fullscreen" },
  // Steps a saga through its state machine on a timed schedule, so visual
  // snapshots would be nondeterministic.
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
} satisfies Meta<typeof SagaVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
