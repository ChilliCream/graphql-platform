import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { AdvisoryHero } from "./AdvisoryHero";

const meta = {
  title: "Pages/Advisory/AdvisoryHero",
  component: AdvisoryHero,
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
} satisfies Meta<typeof AdvisoryHero>;

export default meta;
type Story = StoryObj<typeof meta>;

// The advisory hero with the booking and email CTAs and the headline stat strip.
export const Default: Story = {};
