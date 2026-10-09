import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TierGrid } from "./TierGrid";

const meta = {
  title: "Pages/Advisory/TierGrid",
  component: TierGrid,
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
} satisfies Meta<typeof TierGrid>;

export default meta;
type Story = StoryObj<typeof meta>;

// Consulting highlighted with the rainbow border next to plain Contracting.
export const Default: Story = {};
