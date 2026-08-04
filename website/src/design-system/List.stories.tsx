import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { List, ListItem } from "./List";

const meta = {
  title: "Design System/List",
  component: List,
} satisfies Meta<typeof List>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Unordered: Story = {
  args: { ordered: false },
  render: (args) => (
    <List {...args}>
      <ListItem>First item</ListItem>
      <ListItem>Second item</ListItem>
      <ListItem>Third item</ListItem>
    </List>
  ),
};

export const Ordered: Story = {
  args: { ordered: true },
  render: (args) => (
    <List {...args}>
      <ListItem>Step one</ListItem>
      <ListItem>Step two</ListItem>
      <ListItem>Step three</ListItem>
    </List>
  ),
};

export const Nested: Story = {
  args: { ordered: false },
  render: () => (
    <List>
      <ListItem>Top level item A</ListItem>
      <ListItem>
        Top level item B
        <List>
          <ListItem>Nested item B.1</ListItem>
          <ListItem>Nested item B.2</ListItem>
        </List>
      </ListItem>
      <ListItem>Top level item C</ListItem>
    </List>
  ),
};
