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
              icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="16" height="16"><path d="M326.612 185.391c59.747 59.809 58.927 155.698.36 214.59-.11.12-.24.25-.36.37l-67.2 67.2c-59.27 59.27-155.699 59.262-214.96 0-59.27-59.26-59.27-155.7 0-214.96l37.106-37.106c9.84-9.84 26.786-3.3 27.294 10.606.648 17.722 3.826 35.527 9.69 52.721 1.986 5.822.567 12.262-3.783 16.612l-13.087 13.087c-28.026 28.026-28.905 73.66-1.155 101.96 28.024 28.579 74.086 28.749 102.325.51l67.2-67.19c28.191-28.191 28.073-73.757 0-101.83-3.701-3.694-7.429-6.564-10.341-8.569a16.037 16.037 0 0 1-6.947-12.606c-.396-10.567 3.348-21.456 11.698-29.806l21.054-21.055c5.521-5.521 14.182-6.199 20.584-1.731a152.482 152.482 0 0 1 20.522 17.197zM467.547 44.449c-59.261-59.262-155.69-59.27-214.96 0l-67.2 67.2c-.12.12-.25.25-.36.37-58.566 58.892-59.387 154.781.36 214.59a152.454 152.454 0 0 0 20.521 17.196c6.402 4.468 15.064 3.789 20.584-1.731l21.054-21.055c8.35-8.35 12.094-19.239 11.698-29.806a16.037 16.037 0 0 0-6.947-12.606c-2.912-2.005-6.64-4.875-10.341-8.569-28.073-28.073-28.191-73.639 0-101.83l67.2-67.19c28.239-28.239 74.3-28.069 102.325.51 27.75 28.3 26.872 73.934-1.155 101.96l-13.087 13.087c-4.35 4.35-5.769 10.79-3.783 16.612 5.864 17.194 9.042 34.999 9.69 52.721.509 13.906 17.454 20.446 27.294 10.606l37.106-37.106c59.271-59.259 59.271-155.699.001-214.959z"/></svg>`,
            },
          },
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
