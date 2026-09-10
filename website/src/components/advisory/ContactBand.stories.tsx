import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { ContactBand } from "./ContactBand";

const meta = {
  title: "Pages/Advisory/ContactBand",
  component: ContactBand,
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-6xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof ContactBand>;

export default meta;
type Story = StoryObj<typeof meta>;

// The split contact band with the engagement facts and the booking CTAs.
export const Default: Story = {};
