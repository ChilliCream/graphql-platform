import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { LevelsSection } from "./LevelsSection";

const meta = {
  title: "Pages/Training/LevelsSection",
  component: LevelsSection,
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
} satisfies Meta<typeof LevelsSection>;

export default meta;
type Story = StoryObj<typeof meta>;

// The three team levels (beginner, mixed, advanced) as accented perk cards.
export const Default: Story = {};
