import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from "./Table";

const meta = {
  title: "Design System/Table",
  component: Table,
  argTypes: {
    alternating: {
      control: "boolean",
      description:
        "Tint every other body row to make wide tables easier to scan.",
    },
  },
} satisfies Meta<typeof Table>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: (args) => (
    <Table {...args}>
      <TableHead>
        <TableRow>
          <TableHeaderCell>Name</TableHeaderCell>
          <TableHeaderCell>Role</TableHeaderCell>
          <TableHeaderCell>Stack</TableHeaderCell>
        </TableRow>
      </TableHead>
      <TableBody>
        <TableRow>
          <TableCell>Ada Lovelace</TableCell>
          <TableCell>Engineer</TableCell>
          <TableCell>Analytical Engine</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Grace Hopper</TableCell>
          <TableCell>Compiler Pioneer</TableCell>
          <TableCell>COBOL</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Margaret Hamilton</TableCell>
          <TableCell>Software Engineer</TableCell>
          <TableCell>Apollo Guidance</TableCell>
        </TableRow>
      </TableBody>
    </Table>
  ),
};

export const Alternating: Story = {
  args: { alternating: true },
  render: (args) => (
    <Table {...args}>
      <TableHead>
        <TableRow>
          <TableHeaderCell>Name</TableHeaderCell>
          <TableHeaderCell>Role</TableHeaderCell>
          <TableHeaderCell>Stack</TableHeaderCell>
        </TableRow>
      </TableHead>
      <TableBody>
        <TableRow>
          <TableCell>Ada Lovelace</TableCell>
          <TableCell>Engineer</TableCell>
          <TableCell>Analytical Engine</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Grace Hopper</TableCell>
          <TableCell>Compiler Pioneer</TableCell>
          <TableCell>COBOL</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Margaret Hamilton</TableCell>
          <TableCell>Software Engineer</TableCell>
          <TableCell>Apollo Guidance</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Katherine Johnson</TableCell>
          <TableCell>Mathematician</TableCell>
          <TableCell>Orbital Mechanics</TableCell>
        </TableRow>
      </TableBody>
    </Table>
  ),
};

export const Wide: Story = {
  render: (args) => (
    <Table {...args}>
      <TableHead>
        <TableRow>
          <TableHeaderCell>Product</TableHeaderCell>
          <TableHeaderCell>Description</TableHeaderCell>
          <TableHeaderCell>Latest</TableHeaderCell>
          <TableHeaderCell>Status</TableHeaderCell>
        </TableRow>
      </TableHead>
      <TableBody>
        <TableRow>
          <TableCell>Hot Chocolate</TableCell>
          <TableCell>GraphQL server for .NET</TableCell>
          <TableCell>v15</TableCell>
          <TableCell>Stable</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Strawberry Shake</TableCell>
          <TableCell>GraphQL client for .NET</TableCell>
          <TableCell>v15</TableCell>
          <TableCell>Stable</TableCell>
        </TableRow>
        <TableRow>
          <TableCell>Fusion</TableCell>
          <TableCell>Distributed GraphQL composition</TableCell>
          <TableCell>v15</TableCell>
          <TableCell>Stable</TableCell>
        </TableRow>
      </TableBody>
    </Table>
  ),
};
