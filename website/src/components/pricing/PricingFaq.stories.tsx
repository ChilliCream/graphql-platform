import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PricingFaq } from "./PricingFaq";

const meta = {
  title: "Pages/Pricing/PricingFaq",
  component: PricingFaq,
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
} satisfies Meta<typeof PricingFaq>;

export default meta;
type Story = StoryObj<typeof meta>;

// A two-column grid of disclosures. The whole header row is clickable, not just
// the text or the plus.
export const Default: Story = {};
