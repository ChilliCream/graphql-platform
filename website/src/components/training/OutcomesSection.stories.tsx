import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { OutcomesSection } from "./OutcomesSection";

const meta = {
  title: "Pages/Training/OutcomesSection",
  component: OutcomesSection,
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
} satisfies Meta<typeof OutcomesSection>;

export default meta;
type Story = StoryObj<typeof meta>;

// The six end-of-week outcomes as stacked icon-feature cards.
export const Default: Story = {};
