import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TeamSection } from "./TeamSection";

const meta = {
  title: "Pages/Advisory/TeamSection",
  component: TeamSection,
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
} satisfies Meta<typeof TeamSection>;

export default meta;
type Story = StoryObj<typeof meta>;

// Three credential columns over the Hot Chocolate, Fusion, and Nitro badges.
export const Default: Story = {};
