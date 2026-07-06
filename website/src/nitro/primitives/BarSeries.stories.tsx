import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { BarSeries } from "./BarSeries";
import { ThemeProvider } from "../lib/theme";
import { token } from "../lib/tokens";

const meta = {
  title: "Nitro/Primitives/BarSeries",
  component: BarSeries,
  parameters: { layout: "centered" },
  argTypes: {
    // MotionValue — Storybook can't drive it; the story leaves it undefined so the
    // primitive runs its own clock (pinned to t=1 by reduced motion).
    progress: { control: false },
  },
  args: {
    values: [32, 41, 38, 47, 52, 58, 61, 66, 72, 78, 74, 81, 88, 92, 86, 94],
    color: token.cError,
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        <div className="h-[120px] w-[320px] max-w-full">
          <Story />
        </div>
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof BarSeries>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
