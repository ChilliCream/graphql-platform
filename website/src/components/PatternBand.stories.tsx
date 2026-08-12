import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { PatternBand } from "./PatternBand";
import { SectionHeading } from "./SectionHeading";

const chapterContent = (
  <div className="mx-auto max-w-3xl px-5 text-center sm:px-12">
    <SectionHeading
      align="center"
      size="lg"
      eyebrow="Agentic coding"
      title="Your conventions, enforced automatically"
      description="Skills package the team's working knowledge so every agent starts productive, and changes are reviewed like code."
    />
  </div>
);

const meta = {
  title: "Components/PatternBand",
  component: PatternBand,
  parameters: { layout: "fullscreen" },
  argTypes: {
    pattern: { control: "select", options: ["dots", "grid", "lines"] },
    className: { control: "text" },
    id: { control: "text" },
    flush: { control: "boolean" },
    contain: { control: "boolean" },
    blend: { control: "boolean" },
    recessed: { control: "boolean" },
    recessedBottom: { control: "boolean" },
    children: { control: false },
  },
  args: {
    pattern: "grid",
    className: "border-y py-16 sm:py-24",
    children: chapterContent,
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PatternBand>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Dots: Story = {
  args: {
    pattern: "dots",
  },
};

export const Lines: Story = {
  args: {
    pattern: "lines",
  },
};

export const Blend: Story = {
  args: {
    pattern: "grid",
    blend: true,
    className: "pb-16 sm:pb-24",
  },
};

export const Recessed: Story = {
  args: {
    pattern: "dots",
    recessed: true,
  },
};
