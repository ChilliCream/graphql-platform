import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Typography } from "./Typography";

const meta = {
  title: "Design System/Typography",
  component: Typography,
  argTypes: {
    variant: {
      control: "select",
      options: [
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "body",
        "strong",
        "em",
        "del",
      ],
    },
  },
} satisfies Meta<typeof Typography>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Heading1: Story = {
  args: { variant: "h1", children: "The quick brown fox" },
};

export const Heading2: Story = {
  args: { variant: "h2", children: "The quick brown fox" },
};

export const Heading3: Story = {
  args: { variant: "h3", children: "The quick brown fox" },
};

export const Body: Story = {
  args: {
    variant: "body",
    children:
      "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
  },
};

export const AllHeadings: Story = {
  args: { variant: "h1", children: "" },
  render: () => (
    <div>
      <Typography variant="h1">Heading 1</Typography>
      <Typography variant="h2">Heading 2</Typography>
      <Typography variant="h3">Heading 3</Typography>
      <Typography variant="h4">Heading 4</Typography>
      <Typography variant="h5">Heading 5</Typography>
      <Typography variant="h6">Heading 6</Typography>
    </div>
  ),
};
