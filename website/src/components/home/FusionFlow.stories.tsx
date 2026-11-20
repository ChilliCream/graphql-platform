import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { lightAppearance } from "@/src/components/lightAppearance";

import { FusionFlow } from "./FusionFlow";

const meta = {
  title: "Pages/Home/FusionFlow",
  component: FusionFlow,
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="cc-content-dark">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof FusionFlow>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Dark: Story = {};

export const Light: Story = {
  beforeEach: lightAppearance,
};
