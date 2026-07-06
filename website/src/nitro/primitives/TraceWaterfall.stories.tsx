import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TraceWaterfall } from "./TraceWaterfall";
import { ThemeProvider } from "../lib/theme";
import { makeTrace } from "../lib/data";

const meta = {
  title: "Nitro/Primitives/TraceWaterfall",
  component: TraceWaterfall,
  parameters: { layout: "centered" },
  argTypes: {
    // Master clock is a MotionValue Storybook can't drive; the decorator freezes t=1.
    progress: { control: false },
    style: { control: false },
  },
  args: {
    trace: makeTrace(1),
    rowHeight: 34,
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
} satisfies Meta<typeof TraceWaterfall>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
