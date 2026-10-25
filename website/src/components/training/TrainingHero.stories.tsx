import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TrainingHero } from "./TrainingHero";

const meta = {
  title: "Pages/Training/TrainingHero",
  component: TrainingHero,
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
} satisfies Meta<typeof TrainingHero>;

export default meta;
type Story = StoryObj<typeof meta>;

// The training hero: the headline, the two CTAs, and the starting-line caption.
export const Default: Story = {};
