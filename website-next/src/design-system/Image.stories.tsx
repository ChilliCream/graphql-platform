import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Image } from "./Image";

const meta = {
  title: "Design System/Image",
  component: Image,
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 640 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Image>;

export default meta;
type Story = StoryObj<typeof meta>;

/**
 * When the source fails to load, the image swaps in the same broken-link
 * placeholder used for unavailable videos, sized by the image's intrinsic
 * aspect ratio.
 */
export const Broken: Story = {
  args: {
    src: "https://example.invalid/missing.png",
    alt: "An image that fails to load",
    width: 640,
    height: 360,
  },
};
