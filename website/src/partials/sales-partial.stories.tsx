import React from "react";
import { Story, Meta } from "@storybook/react";
import { SalesPartial } from "./sales-partial";

export default {
  title: "Partials/Sales",
  component: SalesPartial,
} as Meta;

const Template: Story = () => <SalesPartial />;

export const Basic = Template.bind({});
