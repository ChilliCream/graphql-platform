import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ClientPage } from "./ClientPage";

// The whole /products/nitro page, for reviewing the composed layout in Storybook. Its section
// visuals are looping product demos (the reel/standalone screens force reducedMotion="never") plus a
// time-based particle canvas, so the page can't be pinned to a stable frame. It is tagged
// "no-snapshot" so the visual-regression spec skips it (see tests/visual/storybook.spec.ts); the
// individual frozen primitives under "Nitro/*" carry the pixel baselines.
const meta = {
  title: "Pages/Nitro/ProductPage",
  component: ClientPage,
  parameters: { layout: "fullscreen" },
  tags: ["no-snapshot"],
  decorators: [
    (Story) => (
      <div className="cc-content-dark">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ClientPage>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
