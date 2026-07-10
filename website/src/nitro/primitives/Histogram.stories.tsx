import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { Histogram } from "./Histogram";
import { makeHistogram, mulberry32 } from "../lib/data";
import { ThemeProvider } from "../lib/theme";

const meta = {
  title: "Nitro/Primitives/Histogram",
  component: Histogram,
  parameters: { layout: "centered" },
  argTypes: {
    histogram: { control: false },
    progress: { control: false },
    successColor: { control: "color" },
    errorColor: { control: "color" },
  },
  args: {
    histogram: makeHistogram(mulberry32(1)),
    width: 520,
    height: 200,
  },
  decorators: [
    (Story) => (
      <ThemeProvider
        theme="dark"
        reducedMotion="always"
        className="w-[520px] max-w-full p-6"
        style={{ height: 260 }}
      >
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof Histogram>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
