import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { MarkerLine } from "./MarkerLine";
import { ThemeProvider } from "../lib/theme";
import { token } from "../lib/tokens";

const meta = {
  title: "Nitro/Primitives/MarkerLine",
  component: MarkerLine,
  parameters: { layout: "centered" },
  argTypes: {
    // MotionValue shared clock — Storybook cannot control it.
    progress: { control: false },
  },
  args: {
    label: "v2.14.0",
    caption: "deployed",
    at: 0.74,
    color: token.active,
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        {/* MarkerLine is position:relative; 100% x 100%, so it needs a sized
            parent to overlay. A bordered dark panel stands in for the chart. */}
        <div
          style={{
            width: 480,
            height: 200,
            background: token.surface,
            border: `1px solid ${token.border}`,
            borderRadius: 8,
          }}
        >
          <Story />
        </div>
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof MarkerLine>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
