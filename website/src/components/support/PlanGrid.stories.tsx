import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PlanGrid } from "./PlanGrid";

const meta = {
  title: "Pages/Support/PlanGrid",
  component: PlanGrid,
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-6xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof PlanGrid>;

export default meta;
type Story = StoryObj<typeof meta>;

// Four plans, Business highlighted as "Most popular".
export const Default: Story = {};
