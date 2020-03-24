const { createFilePath } = require("gatsby-source-filesystem");
const path = require(`path`);

exports.createPages = async ({ actions, graphql, reporter }) => {
  const { createPage, createRedirect } = actions;
  const blogArticleTemplate = path.resolve(
    `src/templates/blog-article-template.tsx`
  );
  const result = await graphql(`
    {
      blog: allMarkdownRemark(
        limit: 1000
        filter: { frontmatter: { path: { glob: "/blog/**/*" } } }
        sort: { order: DESC, fields: [frontmatter___date] }
      ) {
        posts: edges {
          post: node {
            frontmatter {
              path
            }
          }
        }
        tags: group(field: frontmatter___tags) {
          fieldValue
        }
      }
      docs: allMarkdownRemark(
        limit: 1000
        filter: { frontmatter: { path: { regex: "//docs(/.*)?/" } } }
      ) {
        pages: edges {
          page: node {
            frontmatter {
              path
            }
          }
        }
        navigation: group(field: frontmatter___navigation) {
          fieldValue
        }
      }
    }
  `);

  // Handle errors
  if (result.errors) {
    reporter.panicOnBuild(`Error while running GraphQL query.`);
    return;
  }

  createBlogArticles(createPage, result.data.blog);
  createDocPages(createPage, result.data.docs);

  createRedirect({
    fromPath: "/blog/2019/03/18/entity-framework",
    toPath: "/blog/2020/03/18/entity-framework",
    redirectInBrowser: true,
    isPermanent: true,
  });
};

exports.onCreateNode = ({ node, actions, getNode }) => {
  const { createNodeField } = actions;

  if (node.internal.type === `MarkdownRemark`) {
    const value = createFilePath({ node, getNode });

    createNodeField({
      name: `slug`,
      node,
      value,
    });
  }
};

function createBlogArticles(createPage, data) {
  const blogArticleTemplate = path.resolve(
    `src/templates/blog-article-template.tsx`
  );
  const { posts, tags } = data;

  // Create Single Pages
  posts.forEach(({ post }) => {
    createPage({
      path: post.frontmatter.path,
      component: blogArticleTemplate,
      context: {},
    });
  });

  // Create List Pages
  const postsPerPage = 20;
  const numPages = Math.ceil(posts.length / postsPerPage);

  Array.from({ length: numPages }).forEach((_, i) => {
    createPage({
      path: i === 0 ? `/blog` : `/blog/${i + 1}`,
      component: path.resolve("./src/templates/blog-articles-template.tsx"),
      context: {
        limit: postsPerPage,
        skip: i * postsPerPage,
        numPages,
        currentPage: i + 1,
      },
    });
  });

  // Create Tag Pages
  const tagTemplate = path.resolve(`src/templates/blog-tag-template.tsx`);

  tags.forEach(tag => {
    createPage({
      path: `/blog/tags/${tag.fieldValue}`,
      component: tagTemplate,
      context: {
        tag: tag.fieldValue,
      },
    });
  });
}

function createDocPages(createPage, data) {
  const pageTemplate = path.resolve(`src/templates/doc-page-template.tsx`);
  const { pages } = data;

  // Create Single Pages
  pages.forEach(({ page }) => {
    createPage({
      path: page.frontmatter.path,
      component: pageTemplate,
      context: {},
    });
  });
}
