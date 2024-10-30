const SITE_URL = `https://chillicream.com`;

/** @type import('gatsby').GatsbyConfig */
module.exports = {
  siteMetadata: {
    title: `ChilliCream GraphQL Platform`,
    description: `We help companies and developers to build next level APIs with GraphQL by providing them the right tooling.`,
    author: `Chilli_Cream`,
    company: "ChilliCream",
    siteUrl: SITE_URL,
    repositoryUrl: `https://github.com/ChilliCream/graphql-platform`,
    tools: {
      blog: `/blog`,
      github: `https://github.com/ChilliCream/graphql-platform`,
      linkedIn: `https://www.linkedin.com/company/chillicream`,
      nitro: `https://nitro.chillicream.com`,
      shop: `https://store.chillicream.com`,
      slack: `https://slack.chillicream.com/`,
      youtube: `https://www.youtube.com/c/ChilliCream`,
      x: `https://x.com/Chilli_Cream`,
    },
  },
  plugins: [
    `gatsby-plugin-graphql-codegen`,
    `gatsby-plugin-styled-components`,
    `gatsby-plugin-react-helmet`,
    `gatsby-plugin-robots-txt`,
    `gatsby-plugin-tsconfig-paths`,
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
                sequence: { showSequenceNumbers: true },
              },
            },
          },
          {
            resolve: `gatsby-remark-images`,
            options: {
              maxWidth: 800,
              quality: 100,
              backgroundColor: "transparent",
            },
          },
          {
            resolve: `gatsby-remark-autolink-headers`,
            options: {
              icon: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" width="16" height="16" fill="var(--heading-text-color)"><path d="M326.612 185.391c59.747 59.809 58.927 155.698.36 214.59-.11.12-.24.25-.36.37l-67.2 67.2c-59.27 59.27-155.699 59.262-214.96 0-59.27-59.26-59.27-155.7 0-214.96l37.106-37.106c9.84-9.84 26.786-3.3 27.294 10.606.648 17.722 3.826 35.527 9.69 52.721 1.986 5.822.567 12.262-3.783 16.612l-13.087 13.087c-28.026 28.026-28.905 73.66-1.155 101.96 28.024 28.579 74.086 28.749 102.325.51l67.2-67.19c28.191-28.191 28.073-73.757 0-101.83-3.701-3.694-7.429-6.564-10.341-8.569a16.037 16.037 0 0 1-6.947-12.606c-.396-10.567 3.348-21.456 11.698-29.806l21.054-21.055c5.521-5.521 14.182-6.199 20.584-1.731a152.482 152.482 0 0 1 20.522 17.197zM467.547 44.449c-59.261-59.262-155.69-59.27-214.96 0l-67.2 67.2c-.12.12-.25.25-.36.37-58.566 58.892-59.387 154.781.36 214.59a152.454 152.454 0 0 0 20.521 17.196c6.402 4.468 15.064 3.789 20.584-1.731l21.054-21.055c8.35-8.35 12.094-19.239 11.698-29.806a16.037 16.037 0 0 0-6.947-12.606c-2.912-2.005-6.64-4.875-10.341-8.569-28.073-28.073-28.191-73.639 0-101.83l67.2-67.19c28.239-28.239 74.3-28.069 102.325.51 27.75 28.3 26.872 73.934-1.155 101.96l-13.087 13.087c-4.35 4.35-5.769 10.79-3.783 16.612 5.864 17.194 9.042 34.999 9.69 52.721.509 13.906 17.454 20.446 27.294 10.606l37.106-37.106c59.271-59.259 59.271-155.699.001-214.959z" /></svg>`,
            },
          },
          {
            resolve: require.resolve(`./plugins/gatsby-remark-gather-links`),
          },
          {
            resolve: "gatsby-remark-external-links",
            options: {
              target: "_blank",
              rel: "noopener noreferrer",
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
        name: `basic`,
        path: `${__dirname}/src/basic`,
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
      resolve: require.resolve(`./plugins/gatsby-plugin-validate-links`),
    },
    {
      resolve: `gatsby-plugin-react-svg`,
      options: {
        rule: {
          include: /images/,
          exclude: /images\/(artwork|companies|icons|logo)\/.*\.svg$/,
        },
      },
    },
    {
      resolve: require.resolve(`./plugins/gatsby-plugin-svg-sprite`),
      options: {
        rule: {
          test: /images\/(artwork|companies|icons|logo)\/.*\.svg$/,
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
    `gatsby-plugin-image`,
    {
      resolve: `gatsby-plugin-sharp`,
      options: {
        quality: 100,
      },
    },
    `gatsby-transformer-sharp`,
    {
      resolve: "gatsby-plugin-web-font-loader",
      options: {
        google: {
          families: ["Radio Canada:400,500,600,700"],
        },
      },
    },
    {
      resolve: `gatsby-plugin-manifest`,
      options: {
        name: `ChilliCream GraphQL`,
        short_name: `ChilliCream`,
        start_url: `/`,
        background_color: `#0a0721`,
        theme_color: `#0a0721`,
        display: `standalone`,
        icon: `src/images/chillicream-favicon.png`,
      },
    },
    {
      resolve: `gatsby-plugin-sitemap`,
      options: {
        resolvePagePath({ path }) {
          return `${path}/`.replace("//", "/");
        },
      },
    },
    {
      resolve: `gatsby-plugin-robots-txt`,
      options: {
        host: SITE_URL,
        sitemap: `${SITE_URL}/sitemap-index.xml`,
        policy: [
          {
            userAgent: `*`,
            allow: `/`,
            disallow: [`/docs/hotchocolate/v10/`, `/docs/hotchocolate/v11/`],
          },
          {
            userAgent: `Algolia Crawler`,
            allow: `/`,
          },
        ],
      },
    },
    {
      resolve: `gatsby-plugin-feed`,
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
          const { author, company, description, siteUrl, title } =
            options.query.site.siteMetadata;
          const baseUrl = siteUrl + pathPrefix;
          const currentYear = new Date().getUTCFullYear();

          return {
            ...options,
            id: baseUrl,
            title,
            site_url: baseUrl,
            description,
            copyright: `All rights reserved ${currentYear}, ${company}`,
            author,
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
                    body
                    frontmatter {
                      title
                      author
                      authorUrl
                      date
                      path
                      featuredImage {
                        childImageSharp {
                          gatsbyImageData(layout: CONSTRAINED, width: 800, quality: 100)
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
              allMdx.edges.map(({ node: { excerpt, frontmatter, body } }) => {
                const date = new Date(Date.parse(frontmatter.date));
                const imgSrcPattern = new RegExp(
                  `(${pathPrefix})?/static/`,
                  "g"
                );
                const link = siteUrl + pathPrefix + frontmatter.path;
                let image = frontmatter.featuredImage
                  ? siteUrl +
                    frontmatter.featuredImage.childImageSharp.gatsbyImageData
                      .src
                  : null;

                return {
                  url: link,
                  title: frontmatter.title,
                  date,
                  published: date,
                  description: excerpt,
                  content: body.replace(imgSrcPattern, `${siteUrl}/static/`),
                  image,
                  author: frontmatter.author,
                };
              }),
            title: "ChilliCream Blog",
            output: "/rss.xml",
          },
        ],
      },
    },
    // this (optional) plugin enables Progressive Web App + Offline functionality
    // To learn more, visit: https://gatsby.dev/offline
    // `gatsby-plugin-offline`,
  ],
};
