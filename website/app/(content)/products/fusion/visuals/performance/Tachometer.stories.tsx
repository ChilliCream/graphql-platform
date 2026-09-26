import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ThemeProvider } from "@/src/nitro/lib/theme";

import { Tachometer } from "./Tachometer";

const meta = {
  title: "Products/Fusion/Visuals/Performance/Tachometer",
  component: Tachometer,
  parameters: { layout: "centered" },
  args: {
    max: 20000,
    redZoneStart: 19000,
    settleValue: 18400,
    idleBand: [18100, 18700],
    unit: "ops / min",
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        <div className="w-[240px] max-w-full">
          <Story />
        </div>
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof Tachometer>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Settled: Story = {};
