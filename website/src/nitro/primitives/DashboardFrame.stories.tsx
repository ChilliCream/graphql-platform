import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { DashboardFrame, FULL_WIDTH, SPAN_2 } from "./DashboardFrame";
import { Tile } from "./Tile";
import { CountUp } from "./CountUp";
import { Sparkline } from "./Sparkline";
import { ThemeProvider } from "../lib/theme";

const dashboardChildren = (
  <>
    <Tile
      title="Fleet health"
      subheader="All gateways reporting"
      style={{ ...FULL_WIDTH, minHeight: 72 }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          height: "100%",
          fontSize: 13,
          color: "var(--t-text-secondary)",
        }}
      >
        12 / 12 nodes healthy · last deploy 3m ago
      </div>
    </Tile>

    <Tile title="Requests / min" style={{ minHeight: 116 }}>
      <CountUp value={3120} />
    </Tile>
    <Tile title="p95 latency" style={{ minHeight: 116 }}>
      <CountUp value={84} format={(n) => `${Math.round(n)} ms`} />
    </Tile>
    <Tile title="Error rate" style={{ minHeight: 116 }}>
      <CountUp value={0.4} format={(n) => `${n.toFixed(2)}%`} />
    </Tile>

    <Tile
      title="Throughput"
      subheader="requests / min · last 60 min"
      style={{ ...SPAN_2, minHeight: 128 }}
    >
      <Sparkline
        values={[12, 18, 15, 22, 19, 26, 24, 30, 28, 34, 31, 38]}
        fill
      />
    </Tile>
    <Tile title="Saturation" style={{ minHeight: 128 }}>
      <Sparkline values={[48, 52, 50, 61, 58, 64, 60, 67]} />
    </Tile>
  </>
);

const meta = {
  title: "Nitro/Primitives/DashboardFrame",
  component: DashboardFrame,
  parameters: { layout: "centered" },
  argTypes: {
    children: { control: false },
    progress: { control: false },
    playWindow: { control: false },
    style: { control: false },
  },
  args: {
    children: dashboardChildren,
    gap: 16,
    minColWidth: 320,
    padding: 16,
    animate: true,
  },
  decorators: [
    (Story) => (
      <ThemeProvider
        theme="dark"
        reducedMotion="always"
        className="w-[960px] max-w-full p-6"
      >
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof DashboardFrame>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
