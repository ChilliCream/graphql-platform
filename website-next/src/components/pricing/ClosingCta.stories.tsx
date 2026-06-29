import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ClosingCta } from "./ClosingCta";

const meta = {
  title: "Components/Pricing/ClosingCta",
  component: ClosingCta,
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
} satisfies Meta<typeof ClosingCta>;

export default meta;
type Story = StoryObj<typeof meta>;

// The closing call to action with the spectrum hairline and teal glow.
export const Default: Story = {};
