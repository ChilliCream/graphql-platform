import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { OutboxVisual } from "./OutboxVisual";

const meta = {
  title: "Pages/Mocha/Visuals/OutboxVisual",
  component: OutboxVisual,
  parameters: { layout: "fullscreen" },
  // Runs a timed inbox/outbox transaction loop, so visual snapshots would
  // be nondeterministic.
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
} satisfies Meta<typeof OutboxVisual>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
