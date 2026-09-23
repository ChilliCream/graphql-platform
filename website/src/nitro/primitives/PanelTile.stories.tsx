import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PanelTile } from "./PanelTile";
import { ThemeProvider } from "../lib/theme";
import { token } from "../lib/tokens";

const meta = {
  title: "Nitro/Primitives/PanelTile",
  component: PanelTile,
  parameters: { layout: "centered" },
  argTypes: {
    children: { control: false },
    headerExtra: { control: false },
    subtitle: { control: false },
    style: { control: false },
  },
  args: {
    title: "Latency",
    children: (
      <div style={{ fontSize: 12, color: token.textSecondary }}>
        Chart content goes here
      </div>
    ),
    style: { width: 320, height: 160 },
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
} satisfies Meta<typeof PanelTile>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const WithMetricBadge: Story = {
  args: {
    title: "Throughput",
    headerExtra: (
      <span style={{ display: "flex", alignItems: "baseline", gap: 4 }}>
        <span
          style={{
            fontSize: 16,
            fontWeight: 700,
            fontFamily: token.mono,
            color: token.textStrong,
          }}
        >
          4.9K
        </span>
        <span style={{ fontSize: 10.5, color: token.textSecondary }}>opm</span>
      </span>
    ),
  },
};

export const WithCount: Story = {
  args: {
    title: "Subgraphs",
    count: "6",
    borderStrong: true,
  },
};

export const WithSubtitleAndHeaderExtra: Story = {
  args: {
    title: "Latency Distribution",
    subtitle: "Total operations: 1,204",
    headerExtra: (
      <span style={{ fontSize: 11, color: token.textDim }}>
        Click and drag to select a range
      </span>
    ),
    borderStrong: true,
    bodyPadding: "16px 16px 12px",
    style: { width: 420, height: 200 },
  },
};

export const FixedHeight: Story = {
  args: {
    title: "Latency",
    height: 220,
    flex: undefined,
    headerExtra: (
      <span style={{ fontSize: 11, color: token.textSecondary }}>p95</span>
    ),
    style: { width: 320, height: undefined },
  },
};
