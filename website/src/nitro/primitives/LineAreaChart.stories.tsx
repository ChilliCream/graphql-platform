import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { LineAreaChart } from "./LineAreaChart";
import { ThemeProvider } from "../lib/theme";
import { token } from "../lib/tokens";

const MEAN_SERIES = [
  11, 12, 11, 13, 12, 13, 13, 12, 11, 12, 13, 14, 13, 12, 12, 12, 13, 14, 13,
  11, 11, 12, 13, 12,
];
const P95_SERIES = [
  38, 41, 39, 44, 42, 47, 45, 43, 40, 42, 46, 49, 44, 41, 43, 42, 45, 48, 44,
  40, 39, 42, 44, 41,
];
const P99_SERIES = [
  92, 96, 89, 104, 98, 118, 132, 121, 108, 99, 112, 141, 168, 128, 110, 104,
  118, 152, 137, 112, 101, 108, 116, 106,
];

const meta = {
  title: "Nitro/Primitives/LineAreaChart",
  component: LineAreaChart,
  parameters: { layout: "centered" },
  argTypes: {
    progress: { control: false },
    style: { control: false },
  },
  args: {
    width: 560,
    height: 260,
    domain: [0, 180],
    grid: true,
    showHead: true,
    series: [
      { values: MEAN_SERIES, stroke: token.cLatency, strokeWidth: 1.5 },
      {
        values: P95_SERIES,
        stroke: token.cP95,
        fill: true,
        fillGradient: true,
        fillOpacity: 0.28,
        strokeWidth: 1.5,
      },
      {
        values: P99_SERIES,
        stroke: token.cP99,
        fill: true,
        fillGradient: true,
        fillOpacity: 0.2,
        strokeWidth: 1.5,
      },
    ],
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        <div className="h-[260px] w-[560px] max-w-full">
          <Story />
        </div>
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof LineAreaChart>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
