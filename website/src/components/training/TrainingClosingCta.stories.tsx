import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TrainingClosingCta } from "./TrainingClosingCta";

const meta = {
  title: "Pages/Training/TrainingClosingCta",
  component: TrainingClosingCta,
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
} satisfies Meta<typeof TrainingClosingCta>;

export default meta;
type Story = StoryObj<typeof meta>;

// The closing CTA with the contact address tucked underneath.
export const Default: Story = {};
