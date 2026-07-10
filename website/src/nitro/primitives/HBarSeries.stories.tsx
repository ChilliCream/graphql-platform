import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { HBarSeries } from "./HBarSeries";
import { ThemeProvider } from "../lib/theme";
import { makeClients, mulberry32 } from "../lib/data";

const meta = {
  title: "Nitro/Primitives/HBarSeries",
  component: HBarSeries,
  parameters: { layout: "centered" },
  argTypes: {
    progress: { control: false },
    style: { control: false },
  },
  args: {
    clients: makeClients(mulberry32(1), 5),
    maxBars: 6,
    labelWidth: 96,
    barHeight: 14,
  },
  decorators: [
    (Story) => (
      <ThemeProvider
        theme="dark"
        reducedMotion="always"
        className="w-[360px] max-w-full p-6"
      >
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof HBarSeries>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
