import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ComparisonMatrix } from "./ComparisonMatrix";

const meta = {
  title: "Pages/Support/ComparisonMatrix",
  component: ComparisonMatrix,
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
} satisfies Meta<typeof ComparisonMatrix>;

export default meta;
type Story = StoryObj<typeof meta>;

// Grouped comparison of all four plans.
export const Default: Story = {};
