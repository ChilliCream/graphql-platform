import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { YouTubeVideo } from "./YouTubeVideo";

const meta = {
  title: "Components/YouTube Video",
  component: YouTubeVideo,
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 640 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof YouTubeVideo>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    videoId: "qrh97hToWpM",
  },
};

export const WithPlayLabel: Story = {
  args: {
    videoId: "qrh97hToWpM",
    playlabel: "Play introduction video",
  },
};

/**
 * An invalid id renders the broken-media fallback instead of a player.
 */
export const Broken: Story = {
  args: {
    videoId: "not-an-id",
  },
};
