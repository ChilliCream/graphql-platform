import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { Band } from "./Band";
import { CheckList } from "./CheckList";
import { SectionHeading } from "./SectionHeading";

const splitMain = (
  <SectionHeading
    eyebrow="Enterprise support"
    title="Ship GraphQL with confidence"
    description="Direct access to the people who build Hot Chocolate, with response times your team can plan around."
  />
);

const splitAside = (
  <div className="flex flex-col gap-6">
    <CheckList
      items={[
        "Same-day responses on critical issues",
        "Guided upgrades and migrations",
        "A private channel to the core team",
      ]}
    />
    <div className="flex flex-wrap gap-3">
      <SolidButton href="/contact">Talk to us</SolidButton>
      <OutlineButton href="/pricing">See plans</OutlineButton>
    </div>
  </div>
);

const centeredChildren = (
  <div className="mx-auto max-w-2xl">
    <SectionHeading
      align="center"
      eyebrow="Ready when you are"
      title="Bring Hot Chocolate to production"
      description="Pair with the maintainers to design, review, and ship your GraphQL layer."
    />
    <div className="mt-8 flex flex-wrap justify-center gap-3">
      <SolidButton href="/contact">Start a conversation</SolidButton>
      <OutlineButton href="/docs">Read the docs</OutlineButton>
    </div>
  </div>
);

const meta = {
  title: "Components/Band",
  component: Band,
  parameters: { layout: "fullscreen" },
  argTypes: {
    skin: {
      control: "select",
      options: ["accent", "card", "spectrum", "bare", "warm"],
    },
    layout: { control: "select", options: ["split", "centered"] },
    className: { control: "text" },
    labelledBy: { control: "text" },
    main: { control: false },
    aside: { control: false },
    children: { control: false },
  },
  args: {
    skin: "card",
    layout: "split",
    main: splitMain,
    aside: splitAside,
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-5xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof Band>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Accent: Story = {
  args: { skin: "accent" },
};

export const Warm: Story = {
  args: { skin: "warm" },
};

export const Spectrum: Story = {
  args: { skin: "spectrum" },
};

export const Bare: Story = {
  args: { skin: "bare" },
};

export const Centered: Story = {
  args: {
    skin: "spectrum",
    layout: "centered",
    children: centeredChildren,
  },
};
