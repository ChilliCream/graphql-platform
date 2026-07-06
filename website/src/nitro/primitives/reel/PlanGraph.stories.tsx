import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { motionValue } from "motion/react";

import { PlanGraph } from "./PlanGraph";
import { ThemeProvider } from "../../lib/theme";
import { fusionData } from "../../lib/data/tabs";

// progress is a MotionValue Storybook can't drive; pin it to a constant 1 so the graph renders its
// fully-resolved, executed final frame with no clock running (deterministic screenshot).
const FROZEN_PROGRESS = motionValue(1);

const meta = {
  title: "Nitro/Primitives/PlanGraph",
  component: PlanGraph,
  parameters: { layout: "centered" },
  argTypes: {
    progress: { control: false },
    nodes: { control: false },
    edges: { control: false },
  },
  args: {
    nodes: fusionData.nodes,
    edges: fusionData.edges,
    progress: FROZEN_PROGRESS,
    hoverId: "products",
    fitScale: 0.78,
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" className="max-w-full p-4">
        {/* PlanGraph fills a position:relative box via inset:0; scroll inside, not the page body. */}
        <div style={{ maxWidth: "100%", overflowX: "auto" }}>
          <div
            style={{
              position: "relative",
              width: 1220,
              height: 480,
              borderRadius: 8,
              overflow: "hidden",
            }}
          >
            <Story />
          </div>
        </div>
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof PlanGraph>;

export default meta;
type Story = StoryObj<typeof meta>;

// Every rank revealed and executed (solid orange edges), the hover dwell beat elapsed so nothing
// is dimmed.
export const Default: Story = {};
