module.exports = {
  siteMetadata: {
    title: `ChilliCream GraphQL`,
    description: `...`,
    author: `@Chilli_Cream`,
  },
  plugins: [
    `gatsby-plugin-ts`,
    `gatsby-plugin-styled-components`,
    `gatsby-plugin-react-helmet`,
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
          include: /images\/.*\.svg/,
        },
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
      resolve: `gatsby-plugin-gdpr-cookies`,
      options: {
        googleAnalytics: {
          trackingId: "UA-72800164-1",
          cookieName: "chillicream-gdpr-google-analytics",
          anonymize: true,
        },
        environments: ["production", "development"],
      },
    },
    // this (optional) plugin enables Progressive Web App + Offline functionality
    // To learn more, visit: https://gatsby.dev/offline
    // `gatsby-plugin-offline`,
  ],
};
