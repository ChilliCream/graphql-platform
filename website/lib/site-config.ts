export const siteMetadata = {
  title: "ChilliCream GraphQL Platform",
  description:
    "We help companies and developers to build next level APIs with GraphQL by providing them the right tooling.",
  author: "Chilli_Cream",
  company: "ChilliCream",
  siteUrl: "https://chillicream.com",
  repositoryUrl: "https://github.com/ChilliCream/graphql-platform",
  tools: {
    blog: "/blog",
    github: "https://github.com/ChilliCream/graphql-platform",
    linkedIn: "https://www.linkedin.com/company/chillicream",
    nitro: "https://nitro.chillicream.com",
    shop: "https://store.chillicream.com",
    slack: "https://slack.chillicream.com/",
    youtube: "https://www.youtube.com/c/ChilliCream",
    x: "https://x.com/Chilli_Cream",
  },
};

export type SiteMetadata = typeof siteMetadata;
export type Tools = typeof siteMetadata.tools;
