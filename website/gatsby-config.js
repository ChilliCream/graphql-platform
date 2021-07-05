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
        name: `Developers`,
        items: [
          {
            name: `Docs`,
            link: `/docs/hotchocolate/`,
            icon: `
              <svg style="width:24px;height:24px" viewBox="0 0 24 24">
                  <path fill="currentColor" d="M13,9H18.5L13,3.5V9M6,2H14L20,8V20A2,2 0 0,1 18,22H6C4.89,22 4,21.1 4,20V4C4,2.89 4.89,2 6,2M15,18V16H6V18H15M18,14V12H6V14H18Z" />
              </svg>`,
          },
          {
            name: `Tutorials`,
            link: `/tutorials/hotchocolate/`,
            icon: `
              <svg style="width:24px;height:24px" viewBox="0 0 24 24">
                  <path fill="currentColor" d="M12,3L1,9L12,15L21,10.09V17H23V9M5,13.18V17.18L12,21L19,17.18V13.18L12,17L5,13.18Z" />
              </svg>`,
          },
          {
            name: `Blog`,
            link: `/blog`,
            icon: `
            <svg style="width:24px;height:24px" viewBox="0 0 24 24">
            <path fill="currentColor" d="M4 7V19H19V21H4C2 21 2 19 2 19V7H4M21.3 3H7.7C6.76 3 6 3.7 6 4.55V15.45C6 16.31 6.76 17 7.7 17H21.3C22.24 17 23 16.31 23 15.45V4.55C23 3.7 22.24 3 21.3 3M8 5H13V11H8V5M21 15H8V13H21V15M21 11H15V9H21V11M21 7H15V5H21V7Z" />
        </svg>`,
          },
        ],
      },
      {
        name: `Support`,
        link: `/support`,
      },
      {
        name: `Shop`,
        link: `https://shop.chillicream.com`,
      },
    ],
    tools: {
      github: `https://github.com/ChilliCream/hotchocolate`,
      slack: `https://bit.ly/joinchilli`,
      twitter: `https://twitter.com/Chilli_Cream`,
    },
  },
  plugins: [
    `gatsby-plugin-graphql-codegen`,
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
              backgroundColor: "transparent",
            },
          },
          {
            resolve: `gatsby-remark-autolink-headers`,
            options: {
              icon: `<svg
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 512 512"
                        width="16"
                        height="16"
                      >
                        <path d="M326.612 185.391c59.747 59.809 58.927 155.698.36 214.59-.11.12-.24.25-.36.37l-67.2 67.2c-59.27 59.27-155.699 59.262-214.96 0-59.27-59.26-59.27-155.7 0-214.96l37.106-37.106c9.84-9.84 26.786-3.3 27.294 10.606.648 17.722 3.826 35.527 9.69 52.721 1.986 5.822.567 12.262-3.783 16.612l-13.087 13.087c-28.026 28.026-28.905 73.66-1.155 101.96 28.024 28.579 74.086 28.749 102.325.51l67.2-67.19c28.191-28.191 28.073-73.757 0-101.83-3.701-3.694-7.429-6.564-10.341-8.569a16.037 16.037 0 0 1-6.947-12.606c-.396-10.567 3.348-21.456 11.698-29.806l21.054-21.055c5.521-5.521 14.182-6.199 20.584-1.731a152.482 152.482 0 0 1 20.522 17.197zM467.547 44.449c-59.261-59.262-155.69-59.27-214.96 0l-67.2 67.2c-.12.12-.25.25-.36.37-58.566 58.892-59.387 154.781.36 214.59a152.454 152.454 0 0 0 20.521 17.196c6.402 4.468 15.064 3.789 20.584-1.731l21.054-21.055c8.35-8.35 12.094-19.239 11.698-29.806a16.037 16.037 0 0 0-6.947-12.606c-2.912-2.005-6.64-4.875-10.341-8.569-28.073-28.073-28.191-73.639 0-101.83l67.2-67.19c28.239-28.239 74.3-28.069 102.325.51 27.75 28.3 26.872 73.934-1.155 101.96l-13.087 13.087c-4.35 4.35-5.769 10.79-3.783 16.612 5.864 17.194 9.042 34.999 9.69 52.721.509 13.906 17.454 20.446 27.294 10.606l37.106-37.106c59.271-59.259 59.271-155.699.001-214.959z" />
                      </svg>`,
            },
          },
          {
            resolve: require.resolve(`./plugins/gatsby-remark-gather-links`),
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
        name: `tutorials`,
        path: `${__dirname}/src/tutorials`,
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
    `gatsby-plugin-sharp`,
    `gatsby-transformer-sharp`,
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
                    body
                    frontmatter {
                      title
                      author
                      authorUrl
                      date
                      path
                      featuredImage {
                        childImageSharp {
                          gatsbyImageData(layout: CONSTRAINED, width: 800, pngOptions: { quality: 90 })
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
                    node.frontmatter.featuredImage.childImageSharp
                      .gatsbyImageData.src
                  : null;

                return {
                  id: node.frontmatter.path,
                  link,
                  title: node.frontmatter.title,
                  date,
                  published: date,
                  description: node.excerpt,
                  content: node.body.replace(
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
