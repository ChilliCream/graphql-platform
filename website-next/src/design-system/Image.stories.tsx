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

export const Default: Story = {
  args: {
    src: "https://placehold.co/640x360/png?text=Demo+Image",
    alt: "Demo placeholder image",
    width: 640,
    height: 360,
  },
};

export const Tall: Story = {
  args: {
    src: "https://placehold.co/400x600/png?text=Portrait",
    alt: "Portrait placeholder image",
    width: 400,
    height: 600,
  },
};

/**
 * When the source fails to load, the image swaps in the same broken-link
 * placeholder used for unavailable videos.
 */
export const Broken: Story = {
  args: {
    src: "https://example.invalid/missing.png",
    alt: "An image that fails to load",
    width: 640,
    height: 360,
  },
};
