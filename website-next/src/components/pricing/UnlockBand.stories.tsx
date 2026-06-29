import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { UnlockBand } from "./UnlockBand";

const meta = {
  title: "Components/Pricing/UnlockBand",
  component: UnlockBand,
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
} satisfies Meta<typeof UnlockBand>;

export default meta;
type Story = StoryObj<typeof meta>;

// The spend-threshold progression: each row unlocks more support or deployment
// options as monthly consumption grows.
export const Default: Story = {};
