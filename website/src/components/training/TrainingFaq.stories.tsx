import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TrainingFaq } from "./TrainingFaq";

const meta = {
  title: "Pages/Training/TrainingFaq",
  component: TrainingFaq,
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
} satisfies Meta<typeof TrainingFaq>;

export default meta;
type Story = StoryObj<typeof meta>;

// The pre-booking FAQ rendered with the shared disclosure list.
export const Default: Story = {};
