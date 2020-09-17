module.exports = {
  siteMetadata: {
    title: `ChilliCream GraphQL Platform`,
    description: `We're building the ultimate GraphQL platform`,
    author: `Chilli_Cream`,
    company: "ChilliCream",
    siteUrl: `https://chillicream.com`,
    repositoryUrl: `https://github.com/ChilliCream/hotchocolate`,
    topnav: [
      {
        name: `Platform`,
        link: `/platform`,
      },
      {
        name: `Docs`,
        link: `/docs/hotchocolate/v10/`,
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
          {
            resolve: `gatsby-remark-autolink-headers`,
            options: {
              offsetY: 60,
            },
          },
          `gatsby-remark-reading-time`,
          {
            resolve: `gatsby-remark-code-buttons`,
            options: {
              tooltipText: `Copy`,
              toasterText: "Copied code example",
            },
          },
          {
            resolve: `gatsby-remark-mermaid`,
            options: {
              mermaidOptions: {
                fontFamily: "sans-serif",
              },
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
    {
      resolve: `@darth-knoppix/gatsby-plugin-feed`,
      options: {
        baseUrl: `https://chillicream.com`,
        query: `{
          site {
            siteMetadata {
              title
              description
              siteUrl
              author
              company
            }
            pathPrefix
          }
        }`,
        setup: (options) => {
          const { pathPrefix } = options.query.site;
          const {
            author,
            company,
            description,
            siteUrl,
            title,
          } = options.query.site.siteMetadata;
          const baseUrl = siteUrl + pathPrefix;
          const currentYear = new Date().getUTCFullYear();

          return {
            ...options,
            id: baseUrl,
            title,
            link: baseUrl,
            description,
            copyright: `All rights reserved ${currentYear}, ${company}`,
            author: {
              name: author,
              link: "https://twitter.com/Chilli_Cream",
            },
            generator: "ChilliCream",
            image: `${baseUrl}/favicon-32x32.png`,
            favicon: `${baseUrl}/favicon-32x32.png`,
            feedLinks: {
              atom: `${baseUrl}/atom.xml`,
              json: `${baseUrl}/feed.json`,
              rss: `${baseUrl}/rss.xml`,
            },
            categories: ["GraphQL", "Products", "Services"],
          };
        },
        feeds: [
          {
            query: `{
              allMarkdownRemark(
                limit: 100
                filter: { frontmatter: { path: { regex: "//blog(/.*)?/" } } }
                sort: { order: DESC, fields: [frontmatter___date] },
              ) {
                edges {
                  node {
                    excerpt
                    html
                    frontmatter {
                      title
                      author
                      authorUrl
                      date
                      path
                      featuredImage {
                        childImageSharp {
                          fluid(maxWidth: 800) {
                            src
                          }
                        }
                      }
                    }
                  }
                }
              }
            }`,
            serialize: ({
              query: {
                allMarkdownRemark,
                site: {
                  pathPrefix,
                  siteMetadata: { siteUrl },
                },
              },
            }) =>
              allMarkdownRemark.edges.map(({ node }) => {
                const date = new Date(Date.parse(node.frontmatter.date));
                const imgSrcPattern = new RegExp(
                  `(${pathPrefix})?/static/`,
                  "g"
                );
                const link = siteUrl + pathPrefix + node.frontmatter.path;
                let image = node.frontmatter.featuredImage
                  ? siteUrl +
                    node.frontmatter.featuredImage.childImageSharp.fluid.src
                  : null;

                return {
                  id: node.frontmatter.path,
                  link,
                  title: node.frontmatter.title,
                  date,
                  published: date,
                  description: node.excerpt,
                  content: node.html.replace(
                    imgSrcPattern,
                    `${siteUrl}/static/`
                  ),
                  image,
                  author: [
                    {
                      name: node.frontmatter.author,
                      link: node.frontmatter.authorUrl,
                    },
                  ],
                };
              }),
            title: "ChilliCream Blog",
          },
        ],
      },
    },
    // this (optional) plugin enables Progressive Web App + Offline functionality
    // To learn more, visit: https://gatsby.dev/offline
    // `gatsby-plugin-offline`,
  ],
};
