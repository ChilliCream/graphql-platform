import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { HelpClosing } from "./HelpClosing";

const meta = {
  title: "Pages/Help/HelpClosing",
  component: HelpClosing,
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
} satisfies Meta<typeof HelpClosing>;

export default meta;
type Story = StoryObj<typeof meta>;

// The spectrum closing band with the consultancy and support plan actions.
export const Default: Story = {};
