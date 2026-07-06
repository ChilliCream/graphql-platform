import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ChartPanel } from "./ChartPanel";
import { LineAreaChart } from "./LineAreaChart";
import { ThemeProvider } from "../lib/theme";
import { token } from "../lib/tokens";

const Y_DOMAIN: [number, number] = [0, 300];

const meta = {
  title: "Nitro/Primitives/ChartPanel",
  component: ChartPanel,
  parameters: { layout: "centered" },
  argTypes: {
    // MotionValue / ReactNode / render props: Storybook cannot control these.
    progress: { control: false },
    action: { control: false },
    children: { control: false },
    yFormat: { control: false },
  },
  args: {
    title: "Request latency",
    subtitle: "p50 / p95 / p99",
    legend: [
      { label: "p50", color: token.cLatency, shape: "dot" },
      { label: "p95", color: token.cP95, shape: "dot" },
      { label: "p99", color: token.cP99, shape: "dot" },
    ],
    height: 200,
    yDomain: Y_DOMAIN,
    yTicks: [0, 100, 200, 300],
    yFormat: (n) => `${n}ms`,
    xTicks: ["12:00", "12:15", "12:30", "12:45", "13:00"],
    children: (
      <LineAreaChart
        grid={false}
        domain={Y_DOMAIN}
        series={[
          {
            values: [44, 51, 47, 58, 62, 55, 49, 53, 60, 57, 52, 48],
            stroke: token.cLatency,
            fill: true,
            fillGradient: true,
          },
          {
            values: [
              128, 141, 133, 158, 166, 149, 138, 152, 171, 160, 147, 139,
            ],
            stroke: token.cP95,
          },
          {
            values: [
              212, 231, 224, 248, 259, 240, 226, 244, 261, 250, 237, 229,
            ],
            stroke: token.cP99,
          },
        ]}
      />
    ),
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
} satisfies Meta<typeof ChartPanel>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
