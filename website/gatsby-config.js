module.exports = {
  siteMetadata: {
    title: `ChilliCream GraphQL Platform`,
    description: `We're building the ultimate GraphQL platform`,
    author: `Chilli_Cream`,
    siteUrl: `https://chillicream.github.io`, // todo: set to `https://chillicream.com` before we go online
    repositoryUrl: `https://github.com/ChilliCream/hotchocolate`,
    topnav: [
      {
        name: `Platform`,
        link: `/platform`,
      },
      {
        name: `Docs`,
        link: `/docs/hotchocolate`,
      },
      {
        name: `Support`,
        link: `/support`,
      },
      {
        name: `Blog`,
        link: `/blog`,
      },
      {
        name: `Shop`,
        link: `https://shop.chillicream.com`,
      },
    ],
    tools: {
      github: `https://github.com/ChilliCream/hotchocolate`,
      slack: `https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q`,
      twitter: `https://twitter.com/Chilli_Cream`,
    },
  },
  pathPrefix: `/hotchocolate`, // todo: must be removed before we go online
  plugins: [
    `gatsby-plugin-ts`,
    `gatsby-plugin-styled-components`,
    `gatsby-plugin-react-helmet`,
    {
      resolve: `gatsby-plugin-react-redux`,
      options: {
        pathToCreateStoreModule: `./src/state`,
      },
    },
    {
      resolve: `gatsby-source-filesystem`,
      options: {
        name: `blog`,
        path: `${__dirname}/src/blog`,
      },
    },
    {
      resolve: `gatsby-source-filesystem`,
      options: {
        name: `docs`,
        path: `${__dirname}/src/docs`,
      },
    },
    {
      resolve: `gatsby-source-filesystem`,
      options: {
        name: `images`,
        path: `${__dirname}/src/images`,
      },
    },
    {
      resolve: `gatsby-plugin-react-svg`,
      options: {
        rule: {
          include: /images/,
        },
      },
    },
    {
      resolve: `gatsby-plugin-disqus`,
      options: {
        shortname: `chillicream`,
      },
    },
    `gatsby-transformer-json`,
    {
      resolve: `gatsby-transformer-remark`,
      options: {
        plugins: [
          `gatsby-remark-autolink-headers`,
          `gatsby-remark-reading-time`,
          {
            resolve: `gatsby-remark-mermaid`,
            options: {
              mermaidOptions: {
                fontFamily: "sans-serif",
              },
            },
          },
          {
            resolve: `gatsby-remark-code-buttons`,
            options: {
              tooltipText: `Copy`,
              toasterText: "Copied code example",
            },
          },
          {
            resolve: `gatsby-remark-prismjs`,
            options: {
              showLineNumbers: false,
              inlineCodeMarker: `±`,
              languageExtensions: [
                {
                  language: "sdl",
                  extend: "graphql",
                  definition: {},
                  insertBefore: {},
                },
              ],
            },
          },
          {
            resolve: `gatsby-remark-images`,
            options: {
              maxWidth: 800,
            },
          },
        ],
      },
    },
    `gatsby-transformer-sharp`,
    `gatsby-plugin-sharp`,
    {
      resolve: "gatsby-plugin-web-font-loader",
      options: {
        google: {
          families: ["Roboto"],
        },
      },
    },
    {
      resolve: `gatsby-plugin-manifest`,
      options: {
        name: `ChilliCream GraphQL`,
        short_name: `ChilliCream`,
        start_url: `/`,
        background_color: `#f40010`,
        theme_color: `#f40010`,
        display: `standalone`,
        icon: `src/images/chillicream-favicon.png`,
      },
    },
    {
      resolve: `gatsby-plugin-google-analytics`,
      options: {
        trackingId: "UA-72800164-1",
        anonymize: true,
      },
    },
    `gatsby-plugin-sitemap`,
    // this (optional) plugin enables Progressive Web App + Offline functionality
    // To learn more, visit: https://gatsby.dev/offline
    // `gatsby-plugin-offline`,
    {
      resolve: `gatsby-plugin-feed`,
      options: {
        query: `
          {
            site {
              siteMetadata {
                title
                description
                siteUrl
                site_url: siteUrl
              }
              pathPrefix
            }
          }
        `,
        feeds: [
          {
            serialize: ({ query: { site, allMarkdownRemark } }) => {
              return allMarkdownRemark.edges.map((edge) => {
                const url =
                  site.siteMetadata.siteUrl +
                  site.pathPrefix +
                  edge.node.frontmatter.path;
                return Object.assign({}, edge.node.frontmatter, {
                  description: edge.node.excerpt,
                  date: edge.node.frontmatter.date,
                  url,
                  guid: url,
                  custom_elements: [{ "content:encoded": edge.node.html }],
                });
              });
            },
            query: `
              {
                allMarkdownRemark(
                  limit: 10
                  filter: { frontmatter: { path: { regex: "//blog(/.*)?/" } } }
                  sort: { order: DESC, fields: [frontmatter___date] },
                ) {
                  edges {
                    node {
                      excerpt
                      html
                      fields { slug }
                      frontmatter {
                        title
                        date
                        path
                      }
                    }
                  }
                }
              }
            `,
            output: `/rss.xml`,
            title: `ChilliCream Blog`,
          },
        ],
      },
    },
  ],
};
