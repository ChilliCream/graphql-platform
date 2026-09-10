import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ChartCanvas } from "./ChartCanvas";
import { ThemeProvider } from "../lib/theme";
import { token } from "../lib/tokens";

const meta = {
  title: "Nitro/Primitives/ChartCanvas",
  component: ChartCanvas,
  parameters: { layout: "centered" },
  args: {
    label: "Chart canvas placeholder",
    children: (
      <div
        style={{
          width: "100%",
          height: "100%",
          background: token.surface,
          border: `1px dashed ${token.border}`,
          borderRadius: 6,
        }}
      />
    ),
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        <div className="h-[160px] w-[320px] max-w-full">
          <Story />
        </div>
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof ChartCanvas>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const FillWidth: Story = {
  args: { sizing: "fill-width" },
};

export const None: Story = {
  args: {
    sizing: "none",
    style: { width: 240, height: 120 },
  },
};
