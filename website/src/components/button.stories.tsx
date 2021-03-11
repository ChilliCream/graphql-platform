import React from "react";
import { Story, Meta } from "@storybook/react";
import { Button } from "./button";

export default {
  title: "Components/Button",
  component: Button,
} as Meta;

const Template: Story = (args) => <Button {...args} />;

export const Basic = Template.bind({});
Basic.args = {
  children: "Kuchen",
};
