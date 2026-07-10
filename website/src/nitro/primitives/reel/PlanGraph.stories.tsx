import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { motionValue } from "motion/react";

import { PlanGraph } from "./PlanGraph";
import { ThemeProvider } from "../../lib/theme";
import { fusionData } from "../../lib/data/tabs";

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

export const Default: Story = {};
