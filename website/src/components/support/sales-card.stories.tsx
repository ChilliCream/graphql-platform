import React from "react";
import { Story, Meta } from "@storybook/react";
import { SalesCard, SalesCardProps } from "./sales-card";
import { SalesCardPerk } from "./sales-card-perk";

export default {
  title: "Components/SalesCard",
  component: SalesCard,
} as Meta;

const Template: Story<SalesCardProps> = (args) => (
  <SalesCard {...args}>
    <SalesCardPerk>Potenti felis, in cras at at ligula nunc.</SalesCardPerk>
    <SalesCardPerk>Orci neque eget pellentesque.</SalesCardPerk>
    <SalesCardPerk>Donec mauris sit in eu tincidunt etiam.</SalesCardPerk>
    <SalesCardPerk>Faucibus volutpat magna.</SalesCardPerk>
  </SalesCard>
);

export const Basic = Template.bind({});
Basic.args = {
  name: "Startup",
  description: "All the basics for starting a new business",
  price: 32,
  cycle: "mo",
};
