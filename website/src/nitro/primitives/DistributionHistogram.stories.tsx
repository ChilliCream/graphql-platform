import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { DistributionHistogram } from "./DistributionHistogram";
import { ChartPanel } from "./ChartPanel";
import { makeLatencyDistribution } from "../lib/data";
import { ThemeProvider } from "../lib/theme";

const DISTRIBUTION = makeLatencyDistribution(1);
const MAX_COUNT = Math.max(
  1,
  ...DISTRIBUTION.bins.map((b) => b.success + b.error),
);
const Y_DOMAIN: [number, number] = [1, MAX_COUNT];

const meta = {
  title: "Nitro/Primitives/DistributionHistogram",
  component: DistributionHistogram,
  parameters: { layout: "centered" },
  argTypes: {
    distribution: { control: false },
    progress: { control: false },
    style: { control: false },
    yDomain: { control: "object" },
  },
  args: {
    distribution: DISTRIBUTION,
    yDomain: Y_DOMAIN,
  },
  render: (args) => (
    <ChartPanel
      title="Latency Distribution"
      subtitle={`Total operations: ${args.distribution.total.toLocaleString()}`}
      height={240}
      yLog
      yDomain={args.yDomain}
      yTicks={[1, 10, 100, 1000, 10000]}
      xTicks={["1ms", "10ms", "100ms", "1s", "10s"]}
    >
      <DistributionHistogram {...args} />
    </ChartPanel>
  ),
  decorators: [
    (Story) => (
      <ThemeProvider
        theme="dark"
        reducedMotion="always"
        className="w-[720px] max-w-full p-6"
      >
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof DistributionHistogram>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
