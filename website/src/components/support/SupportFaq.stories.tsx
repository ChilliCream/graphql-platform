import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { SupportFaq } from "./SupportFaq";

const meta = {
  title: "Pages/Support/SupportFaq",
  component: SupportFaq,
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
} satisfies Meta<typeof SupportFaq>;

export default meta;
type Story = StoryObj<typeof meta>;

// Exclusive accordion: opening one answer closes the others.
export const Default: Story = {};
