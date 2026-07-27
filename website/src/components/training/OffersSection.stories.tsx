import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { OffersSection } from "./OffersSection";

const meta = {
  title: "Pages/Training/OffersSection",
  component: OffersSection,
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
} satisfies Meta<typeof OffersSection>;

export default meta;
type Story = StoryObj<typeof meta>;

// The two corporate offers: training to align, workshop to ship (highlighted).
export const Default: Story = {};
