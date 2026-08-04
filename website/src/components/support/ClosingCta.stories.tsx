import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ClosingCta } from "./ClosingCta";

const meta = {
  title: "Pages/Support/ClosingCta",
  component: ClosingCta,
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
} satisfies Meta<typeof ClosingCta>;

export default meta;
type Story = StoryObj<typeof meta>;

// The closing community Slack / contact-sales call to action.
export const Default: Story = {};
