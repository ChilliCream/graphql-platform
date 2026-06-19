import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Image } from "./Image";

const meta = {
  title: "Design System/Avatar",
  component: Image,
} satisfies Meta<typeof Image>;

export default meta;
type Story = StoryObj<typeof meta>;

/**
 * A small, round avatar image.
 */
export const Avatar: Story = {
  args: {
    src: "https://picsum.photos/seed/avatar/96/96",
    alt: "User avatar",
    width: 96,
    height: 96,
    className: "h-24 w-24 rounded-full object-cover",
  },
};

/**
 * In tiny frames like avatars, the icon shrinks and the message is kept for
 * screen readers only.
 */
export const BrokenAvatar: Story = {
  args: {
    src: "https://example.invalid/missing.png",
    alt: "An avatar that fails to load",
    width: 96,
    height: 96,
    className: "h-24 w-24 rounded-full object-cover",
  },
};
