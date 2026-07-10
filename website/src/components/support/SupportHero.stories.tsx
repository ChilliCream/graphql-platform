import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { SupportHero } from "./SupportHero";

const meta = {
  title: "Pages/Support/SupportHero",
  component: SupportHero,
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
} satisfies Meta<typeof SupportHero>;

export default meta;
type Story = StoryObj<typeof meta>;

// The hero with the three support scenarios: a question, an outage, a session.
export const Default: Story = {};
