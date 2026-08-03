import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { Legend } from "./Legend";
import { ThemeProvider } from "../lib/theme";
import { token } from "../lib/tokens";

const meta = {
  title: "Nitro/Primitives/Legend",
  component: Legend,
  parameters: { layout: "centered" },
  argTypes: {
    items: { control: "object" },
    style: { control: false },
  },
  args: {
    items: [
      { label: "p95 latency", color: token.cSuccess, shape: "dot" },
      { label: "p99 latency", color: token.info, shape: "ring", muted: true },
      { label: "Errors", color: token.cError, shape: "square" },
    ],
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
} satisfies Meta<typeof Legend>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
