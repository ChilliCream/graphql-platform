import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { CountUp } from "./CountUp";
import { ThemeProvider } from "../lib/theme";

const meta = {
  title: "Nitro/Primitives/CountUp",
  component: CountUp,
  parameters: { layout: "centered" },
  argTypes: {
    format: { control: false },
    progress: { control: false },
    playWindow: { control: false },
    style: { control: false },
  },
  args: {
    value: 3120,
    from: 0,
  },
  decorators: [
    (Story) => (
      <ThemeProvider
        theme="dark"
        reducedMotion="always"
        className="w-[560px] max-w-full p-6"
      >
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof CountUp>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
