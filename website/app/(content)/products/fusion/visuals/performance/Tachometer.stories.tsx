import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ThemeProvider } from "@/src/nitro/lib/theme";

import { Tachometer } from "./Tachometer";

const meta = {
  title: "Products/Fusion/Visuals/Performance/Tachometer",
  component: Tachometer,
  parameters: { layout: "centered" },
  args: {
    active: false,
    reduced: true,
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" className="p-6">
        <div className="w-[280px] max-w-full">
          <Story />
        </div>
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof Tachometer>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Settled: Story = {};
