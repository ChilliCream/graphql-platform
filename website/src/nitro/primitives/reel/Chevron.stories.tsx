import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { Chevron } from "./Chevron";
import { ThemeProvider } from "../../lib/theme";

const meta = {
  title: "Nitro/Primitives/Chevron",
  component: Chevron,
  parameters: { layout: "centered" },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof Chevron>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Down: Story = {};

export const Up: Story = {
  args: {
    up: true,
  },
};
