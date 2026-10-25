import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { HeroBoard } from "./HeroBoard";

const meta = {
  title: "Mocha/HeroBoard",
  component: HeroBoard,
  parameters: { layout: "fullscreen" },
  // The board pre-renders once but then runs a canvas rAF loop (breathing
  // node halos, travelling message pulses), so visual snapshots would be
  // nondeterministic.
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark relative h-[640px] w-full overflow-hidden">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof HeroBoard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
