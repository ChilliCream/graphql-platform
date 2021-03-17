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
        link: `/docs/hotchocolate/`,
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
      slack: `http://bit.ly/joinchilli`,
      twitter: `https://twitter.com/Chilli_Cream`,
    },
  },
  plugins: [
    `gatsby-plugin-ts`,
    `gatsby-plugin-styled-components`,
    `gatsby-plugin-react-helmet`,
    `gatsby-remark-reading-time`,
    {
      resolve: `gatsby-plugin-mdx`,
      options: {
        extensions: [`.mdx`, `.md`],
        gatsbyRemarkPlugins: [
          {
            resolve: `gatsby-remark-mermaid`,
            options: {
              mermaidOptions: {
                fontFamily: "sans-serif",
              },
            },
          },
          {
            resolve: `gatsby-remark-images`,
            options: {
              maxWidth: 800,
              quality: 90,
              backgroundColor: 'transparent',
            },
          },
        ],
      },
    },
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
              allMdx(
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
                          fluid(maxWidth: 800, pngQuality: 90) {
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
                allMdx,
                site: {
                  pathPrefix,
                  siteMetadata: { siteUrl },
                },
              },
            }) =>
              allMdx.edges.map(({ node }) => {
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
