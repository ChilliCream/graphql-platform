import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { Tile } from "./Tile";
import { CountUp } from "./CountUp";
import { Sparkline } from "./Sparkline";
import { ThemeProvider } from "../lib/theme";

const meta = {
  title: "Nitro/Primitives/Tile",
  component: Tile,
  parameters: { layout: "centered" },
  argTypes: {
    // ReactNode / MotionValue / tuple / object props can't be driven by controls.
    children: { control: false },
    action: { control: false },
    progress: { control: false },
    playWindow: { control: false },
    style: { control: false },
  },
  args: {
    title: "Total Requests",
    subheader: "last 24h",
    // Body: a big stat over a trend line — the tile's real content, shown once the
    // reduced-motion decorator pins the clock to t=1 (skeleton hidden, content in).
    children: (
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          justifyContent: "space-between",
          height: "100%",
        }}
      >
        <CountUp value={128400} />
        <Sparkline
          values={[42, 55, 48, 61, 74, 69, 88, 95, 112, 108, 121, 128]}
        />
      </div>
    ),
    style: { width: 300, height: 160 },
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
} satisfies Meta<typeof Tile>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
