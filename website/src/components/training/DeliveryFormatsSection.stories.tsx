import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { DeliveryFormatsSection } from "./DeliveryFormatsSection";

const meta = {
  title: "Pages/Training/DeliveryFormatsSection",
  component: DeliveryFormatsSection,
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
} satisfies Meta<typeof DeliveryFormatsSection>;

export default meta;
type Story = StoryObj<typeof meta>;

// On site, remote, and hybrid delivery formats as inline icon-feature cards.
export const Default: Story = {};
