import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Pagination } from "./Pagination";

const meta = {
  title: "Design System/Pagination",
  component: Pagination,
  args: {
    hrefForPage: (p: number) => `?page=${p}`,
  },
} satisfies Meta<typeof Pagination>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Few: Story = {
  args: { currentPage: 2, totalPages: 4 },
};

export const Many: Story = {
  args: { currentPage: 7, totalPages: 24 },
};

export const FirstPage: Story = {
  args: { currentPage: 1, totalPages: 12 },
};

export const LastPage: Story = {
  args: { currentPage: 12, totalPages: 12 },
};

export const SinglePage: Story = {
  args: { currentPage: 1, totalPages: 1 },
};
