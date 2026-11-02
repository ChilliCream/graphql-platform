import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { Sparkline } from "./Sparkline";
import { ThemeProvider } from "../lib/theme";

const latencySeries = [42, 48, 45, 51, 58, 54, 49, 46, 52, 60, 55];

const meta = {
  title: "Nitro/Primitives/Sparkline",
  component: Sparkline,
  parameters: { layout: "centered" },
  argTypes: {
    progress: { control: false },
    style: { control: false },
  },
  args: {
    values: latencySeries,
    height: 40,
    width: 160,
    fill: false,
    style: { width: 160, height: 40 },
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof Sparkline>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Filled: Story = {
  args: { fill: true },
};
