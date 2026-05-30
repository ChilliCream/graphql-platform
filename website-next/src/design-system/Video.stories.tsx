import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Video } from "./Video";
import { VideoFacade } from "./VideoFacade";

const meta = {
  title: "Design System/Video",
  component: Video,
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 640 }}>
        <Story />
      </div>
    ),
  ],
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

export const Broken: Story = {
  args: {
    src: "https://example.com/not-a-video",
  },
};

/**
 * A non-YouTube source: the facade uses a custom poster and our accent play
 * button (instead of YouTube red), reverse-confirming the provider styling.
 */
export const NonYouTubePlaceholder: Story = {
  name: "Non-YouTube (placeholder)",
  args: { src: "" },
  render: () => (
    <div className="overflow-hidden rounded-md ring-1 ring-cc-card-border">
      <VideoFacade
        provider="generic"
        poster="https://placehold.co/1280x720/0c1322/f5f1ea?text=Sample+Video"
        embedSrc="https://www.w3schools.com/html/mov_bbb.mp4"
        playlabel="Play sample video"
      />
    </div>
  ),
};
