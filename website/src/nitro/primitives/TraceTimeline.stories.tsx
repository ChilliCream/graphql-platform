import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { TraceTimeline } from "./TraceTimeline";
import { makeTraceSamples } from "../lib/data";
import { ThemeProvider } from "../lib/theme";

const meta = {
  title: "Nitro/Primitives/TraceTimeline",
  component: TraceTimeline,
  parameters: { layout: "centered" },
  argTypes: {
    // driven by the Nitro chart clock — not a Storybook-controllable value
    progress: { control: false },
    playWindow: { control: false },
    style: { control: false },
    samples: { control: false },
  },
  args: {
    samples: makeTraceSamples(1, 90),
    width: 600,
    height: 240,
    threshold: 260,
    showScan: true,
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        <div className="h-[260px] w-[600px] max-w-full">
          <Story />
        </div>
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof TraceTimeline>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
