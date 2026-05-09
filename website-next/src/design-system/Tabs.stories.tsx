import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Tab, Tabs } from "./Tabs";

const meta = {
  title: "Design System/Tabs",
  component: Tabs,
  argTypes: {
    defaultIndex: {
      control: { type: "number", min: 0 },
    },
  },
} satisfies Meta<typeof Tabs>;

export default meta;
type Story = StoryObj<typeof meta>;

export const StartingOnSecondTab: Story = {
  args: {
    defaultIndex: 1,
    children: [
      <Tab key="first" label="First">
        First tab content.
      </Tab>,
      <Tab key="second" label="Second">
        Second tab content (selected by default).
      </Tab>,
      <Tab key="third" label="Third">
        Third tab content.
      </Tab>,
    ],
  },
};

export const ManyTabs: Story = {
  args: {
    children: Array.from({ length: 8 }, (_, i) => (
      <Tab key={i} label={`Tab ${i + 1}`}>
        Content for tab {i + 1}. The tab strip wraps when there's not enough
        horizontal room.
      </Tab>
    )),
  },
};

export const SingleTab: Story = {
  args: {
    children: (
      <Tab label="Only one">
        A single tab still renders cleanly. The strip degrades gracefully.
      </Tab>
    ),
  },
};
