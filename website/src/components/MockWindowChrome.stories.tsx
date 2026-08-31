import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { MockWindowChrome } from "./MockWindowChrome";

const placeholderBody = (
  <div className="text-cc-ink-dim p-6 font-mono text-xs">window content</div>
);

const meta = {
  title: "Components/MockWindowChrome",
  component: MockWindowChrome,
  parameters: { layout: "fullscreen" },
  argTypes: {
    shadow: { control: "select", options: ["2xl", "soft", "none"] },
    rounded: { control: "text" },
    className: { control: "text" },
    surfaceClassName: { control: "text" },
    headerClassName: { control: "text" },
    footerClassName: { control: "text" },
    header: { control: false },
    label: { control: "text" },
    headerRight: { control: false },
    footer: { control: false },
    glow: { control: false },
    children: { control: false },
  },
  args: {
    children: placeholderBody,
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof MockWindowChrome>;

export default meta;
type Story = StoryObj<typeof meta>;

/** NitroSection's SurfaceCard: traffic-light dots, glow halo, shadow-2xl. */
export const Dots: Story = {
  args: {
    header: { variant: "dots" },
    label: "Nitro / Author",
    glow: {
      background:
        "radial-gradient(60% 60% at 50% 35%, rgba(94,234,212,0.16), transparent 70%)",
      inset: "-inset-3",
      blur: "blur-2xl",
      rounded: "rounded-[2rem]",
    },
    rounded: "rounded-xl",
  },
};

/** ControlPlaneConsole: single status dot, trailing label, glow halo, shadow-2xl. */
export const StatusDot: Story = {
  args: {
    header: { variant: "status-dot" },
    label: "Nitro / production",
    headerRight: (
      <span className="text-cc-ink-dim font-mono text-[10px]">live</span>
    ),
    glow: {
      background:
        "radial-gradient(50% 50% at 50% 30%, rgba(94,234,212,0.16), transparent 70%)",
      inset: "-inset-x-10 -inset-y-8",
      blur: "blur-3xl",
      rounded: "rounded-[2.5rem]",
    },
  },
};

/** ClientPage's FramedVisual: no header bar, glow halo, shadow-2xl. */
export const NoHeader: Story = {
  args: {
    glow: {
      background:
        "radial-gradient(60% 60% at 50% 40%, rgba(22,185,228,0.16), transparent 70%)",
      inset: "-inset-x-6 -inset-y-4",
      blur: "blur-3xl",
      rounded: "rounded-[2rem]",
    },
  },
};

/** AgenticSection's SkillFacet: badge+filename header, matching footer bar, no glow/shadow-2xl. */
export const HeaderAndFooter: Story = {
  args: {
    shadow: "none",
    surfaceClassName: "bg-cc-card-bg",
    headerClassName:
      "bg-cc-surface/40 flex items-center justify-between gap-2.5 px-3 py-2.5",
    header: {
      variant: "custom",
      content: (
        <span className="inline-flex items-center gap-2">
          <span
            className="inline-flex size-[18px] items-center justify-center rounded-[5px] font-mono text-[0.5rem] font-bold"
            style={{
              background: "rgba(139, 143, 240, 0.14)",
              border: "1px solid rgba(139, 143, 240, 0.4)",
              color: "#8b8ff0",
            }}
          >
            MD
          </span>
          <span className="font-mono text-xs">
            <span className="text-cc-heading">SKILL</span>
            <span className="text-cc-ink-dim">.md</span>
          </span>
        </span>
      ),
    },
    headerRight: (
      <span className="text-cc-nav-label font-mono text-[0.6rem] whitespace-nowrap">
        skills/
      </span>
    ),
    footerClassName:
      "bg-cc-surface/40 flex items-center justify-between gap-2.5 px-3 py-1.5",
    footer: (
      <>
        <span className="text-cc-nav-label font-mono text-[0.6rem]">main</span>
        <span className="text-cc-ink-dim font-mono text-[0.6rem]">
          reviewed
        </span>
      </>
    ),
  },
};

/** ReviewSection's PR window: header bar with branch + mono label + status badge, custom drop-shadow, no glow. */
export const CustomHeaderSoftShadow: Story = {
  args: {
    shadow: "soft",
    headerClassName:
      "flex flex-wrap items-center gap-x-3 gap-y-2 bg-white/[0.03] px-4 py-2.5",
    header: {
      variant: "custom",
      content: (
        <>
          <span className="text-cc-ink font-mono text-sm">
            feat: add product reviews
          </span>
          <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.1em] uppercase">
            3 files changed
          </span>
          <span className="border-cc-success/40 bg-cc-success/10 text-cc-success ml-auto inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 font-mono text-[0.65rem] tracking-[0.1em] uppercase">
            Approved
          </span>
        </>
      ),
    },
  },
};
