import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { HelpHero } from "./HelpHero";

const meta = {
  title: "Pages/Help/HelpHero",
  component: HelpHero,
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
} satisfies Meta<typeof HelpHero>;

export default meta;
type Story = StoryObj<typeof meta>;

// The hero with the consultancy and Slack call to action.
export const Default: Story = {};
