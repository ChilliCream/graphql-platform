import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { HelpFaq } from "./HelpFaq";

const meta = {
  title: "Pages/Help/HelpFaq",
  component: HelpFaq,
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
} satisfies Meta<typeof HelpFaq>;

export default meta;
type Story = StoryObj<typeof meta>;

// The help FAQ disclosure list.
export const Default: Story = {};
