import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Video } from "./Video";

const meta = {
  title: "Design System/Video",
  component: Video,
} satisfies Meta<typeof Video>;

export default meta;
type Story = StoryObj<typeof meta>;

export const FromVideoId: Story = {
  args: {
    src: "qrh97hToWpM",
    playlabel: "Play introduction video",
  },
};

export const FromWatchUrl: Story = {
  args: {
    src: "https://www.youtube.com/watch?v=qrh97hToWpM",
  },
};

export const FromShortUrl: Story = {
  args: {
    src: "https://youtu.be/qrh97hToWpM",
  },
};

export const InvalidSrc: Story = {
  args: {
    src: "https://example.com/not-a-video",
  },
};
