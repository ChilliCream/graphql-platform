import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Image } from "./Image";

const meta = {
  title: "Design System/Image",
  component: Image,
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
