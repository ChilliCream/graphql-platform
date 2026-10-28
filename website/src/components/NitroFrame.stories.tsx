import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { lightAppearance } from "@/src/components/lightAppearance";
import { ChartPanel } from "@/src/nitro/primitives/ChartPanel";
import { LineAreaChart } from "@/src/nitro/primitives/LineAreaChart";
import { token } from "@/src/nitro/lib/tokens";

import { NitroFrame } from "./NitroFrame";

const Y_DOMAIN: [number, number] = [0, 300];

const meta = {
  title: "Components/NitroFrame",
  component: NitroFrame,
  parameters: { layout: "fullscreen" },
  args: {
    reducedMotion: "always",
    className: "w-[560px] max-w-full p-6",
    children: (
      <ChartPanel
        title="Request latency"
        subtitle="p50 / p99"
        legend={[
          { label: "p50", color: token.cLatency, shape: "dot" },
          { label: "p99", color: token.cP99, shape: "dot" },
        ]}
        height={200}
        yDomain={Y_DOMAIN}
        yTicks={[0, 100, 200, 300]}
        yFormat={(n) => `${n}ms`}
        xTicks={["12:00", "12:30", "13:00"]}
      >
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
                212, 231, 224, 248, 259, 240, 226, 244, 261, 250, 237, 229,
              ],
              stroke: token.cP99,
            },
          ]}
        />
      </ChartPanel>
    ),
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-6">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof NitroFrame>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Dark: Story = {};

export const Light: Story = {
  beforeEach: lightAppearance,
};
