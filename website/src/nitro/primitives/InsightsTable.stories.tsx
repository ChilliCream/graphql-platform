import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { InsightsTable } from "./InsightsTable";
import { ThemeProvider } from "../lib/theme";
import { makeInsights, mulberry32 } from "../lib/data";

const meta = {
  title: "Nitro/Primitives/InsightsTable",
  component: InsightsTable,
  parameters: { layout: "centered" },
  argTypes: {
    // Master clock is a MotionValue Storybook can't drive; the decorator freezes t=1.
    progress: { control: false },
    rows: { control: false },
    style: { control: false },
  },
  args: {
    rows: makeInsights(mulberry32(1), 7),
    errorThreshold: 0.03,
    rowStagger: 0.1,
  },
  decorators: [
    (Story) => (
      <ThemeProvider
        theme="dark"
        reducedMotion="always"
        className="w-[640px] max-w-full p-6"
      >
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof InsightsTable>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
