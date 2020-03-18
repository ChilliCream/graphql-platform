const path = require(`path`);

exports.createPages = async ({ actions, graphql, reporter }) => {
  const { createPage, createRedirect } = actions;
  const blogArticleTemplate = path.resolve(`src/templates/blog-article.tsx`);
  const result = await graphql(`
    {
      allMarkdownRemark(
        sort: { order: DESC, fields: [frontmatter___date] }
        limit: 1000
      ) {
        edges {
          node {
            frontmatter {
              path
            }
          }
        }
      }
    }
  `);

  // Handle errors
  if (result.errors) {
    reporter.panicOnBuild(`Error while running GraphQL query.`);
    return;
  }

  result.data.allMarkdownRemark.edges.forEach(({ node }) => {
    createPage({
      path: node.frontmatter.path,
      component: blogArticleTemplate,
      context: {}, // additional data can be passed via context
    });
  });

  createRedirect({
    fromPath: "/blog/2019/03/18/entity-framework",
    toPath: "/blog/2020/03/18/entity-framework",
    redirectInBrowser: true,
    isPermanent: true,
  });
};
