import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { HelpTiers } from "./HelpTiers";

const meta = {
  title: "Pages/Help/HelpTiers",
  component: HelpTiers,
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
} satisfies Meta<typeof HelpTiers>;

export default meta;
type Story = StoryObj<typeof meta>;

// Three paths, Support highlighted as "Best Value".
export const Default: Story = {};
