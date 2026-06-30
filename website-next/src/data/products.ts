export type Product = {
  slug: string;
  title: string;
  description: string;
};

export const PRODUCTS: readonly Product[] = [
  {
    slug: "nitro",
    title: "Nitro",
    description: "GraphQL API Management",
  },
  {
    slug: "hotchocolate",
    title: "Hot Chocolate",
    description: "GraphQL Server for .NET",
  },
  {
    slug: "fusion",
    title: "Fusion",
    description: "Federated GraphQL Gateway",
  },
  {
    slug: "strawberryshake",
    title: "Strawberry Shake",
    description: "GraphQL Client for .NET",
  },
  {
    slug: "mocha",
    title: "Mocha",
    description: "Messaging Bus for .NET",
  },
  {
    slug: "skillz",
    title: "Skillz",
    description: "Agent Skills CLI for .NET",
  },
];
