import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PlanSelector } from "./PlanSelector";

const meta = {
  title: "Pages/Pricing/PlanSelector",
  component: PlanSelector,
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-5xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof PlanSelector>;

export default meta;
type Story = StoryObj<typeof meta>;

// The three cloud tiers as Offering cards with the self-hosted strip below.
// Dedicated is highlighted with the rainbow gradient border ("Most Popular").
export const Default: Story = {};
