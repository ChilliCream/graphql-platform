import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { AdvisoryFaq } from "./AdvisoryFaq";

const meta = {
  title: "Pages/Advisory/AdvisoryFaq",
  component: AdvisoryFaq,
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
} satisfies Meta<typeof AdvisoryFaq>;

export default meta;
type Story = StoryObj<typeof meta>;

// The advisory FAQ as the shared accordion disclosure list.
export const Default: Story = {};
