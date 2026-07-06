import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { AppMotionConfig } from "@/src/nitro/lib/motion";

import { ControlPlaneConsole } from "./ControlPlaneConsole";

// ControlPlaneConsole animates its KPIs, sparkline, trace waterfall and schema diff in a loop, so a
// plain story would screenshot a non-deterministic frame. `AppMotionConfig reducedMotion="always"`
// drives its `useReducedMotionPreference()` to the reduced path, which pins every animation to its
// final frame (fully-drawn dashboard) for a stable screenshot.
const meta = {
  title: "Nitro/ControlPlaneConsole",
  component: ControlPlaneConsole,
  parameters: { layout: "fullscreen" },
  argTypes: {
    className: { control: "text" },
  },
  args: {
    className: "mx-auto max-w-5xl",
  },
  decorators: [
    (Story) => (
      <AppMotionConfig reducedMotion="always">
        <div className="cc-content-dark p-10">
          <Story />
        </div>
      </AppMotionConfig>
    ),
  ],
} satisfies Meta<typeof ControlPlaneConsole>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
