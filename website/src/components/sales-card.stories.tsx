import React from "react";
import { Story, Meta } from "@storybook/react";
import { SalesCard, SalesCardProps } from "./sales-card";

export default {
  title: "Chillicream/SalesCard",
  component: SalesCard,
} as Meta;

const Template: Story<SalesCardProps> = (args) => <SalesCard {...args} />;

export const Basic = Template.bind({});
Basic.args = {
  name: "Startup",
  description: "All the basics for starting a new business",
  price: 32,
  perks: [
    "Potenti felis, in cras at at ligula nunc.",
    "Orci neque eget pellentesque.",
    "Donec mauris sit in eu tincidunt etiam.",
    "Faucibus volutpat magna.",
  ],
};
