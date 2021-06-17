const { createFilePath } = require("gatsby-source-filesystem");
const path = require("path");
const git = require("simple-git/promise");

exports.createPages = async ({ actions, graphql, reporter }) => {
  const { createPage, createRedirect } = actions;
  const result = await graphql(`
    {
      blog: allMdx(
        limit: 1000
        filter: { frontmatter: { path: { regex: "//blog(/.*)?/" } } }
        sort: { order: DESC, fields: [frontmatter___date] }
      ) {
        posts: nodes {
          fields {
            slug
          }
        }
        tags: group(field: frontmatter___tags) {
          fieldValue
        }
      }
      docs: allFile(
        limit: 1000
        filter: { sourceInstanceName: { eq: "docs" }, extension: { eq: "md" } }
      ) {
        pages: nodes {
          name
          relativeDirectory
          childMdx {
            fields {
              slug
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

  createBlogArticles(createPage, result.data.blog);
  createDocPages(createPage, result.data.docs);

  createRedirect({
    fromPath: "/blog/2019/03/18/entity-framework",
    toPath: "/blog/2020/03/18/entity-framework",
    redirectInBrowser: true,
    isPermanent: true,
  });
  createRedirect({
    fromPath: "/docs/",
    toPath: "/docs/hotchocolate/",
    redirectInBrowser: true,
    isPermanent: true,
  });
  createRedirect({
    fromPath: "/docs/marshmallowpie/",
    toPath: "/docs/hotchocolate/",
    redirectInBrowser: true,
    isPermanent: true,
  });

  // images
  createRedirect({
    fromPath: "/img/projects/greendonut-banner.svg",
    toPath: "/resources/greendonut-banner.svg",
    redirectInBrowser: true,
    isPermanent: true,
  });
  createRedirect({
    fromPath: "/img/projects/greendonut-signet.png",
    toPath: "/resources/greendonut-signet.png",
    redirectInBrowser: true,
    isPermanent: true,
  });
  createRedirect({
    fromPath: "/img/projects/hotchocolate-banner.svg",
    toPath: "/resources/hotchocolate-banner.svg",
    redirectInBrowser: true,
    isPermanent: true,
  });
  createRedirect({
    fromPath: "/img/projects/hotchocolate-signet.png",
    toPath: "/resources/hotchocolate-signet.png",
    redirectInBrowser: true,
    isPermanent: true,
  });
  createRedirect({
    fromPath: "/img/projects/react-rasta-banner.svg",
    toPath: "/resources/react-rasta-banner.svg",
    redirectInBrowser: true,
    isPermanent: true,
  });
  createRedirect({
    fromPath: "/img/projects/react-rasta-signet.png",
    toPath: "/resources/react-rasta-signet.png",
    redirectInBrowser: true,
    isPermanent: true,
  });
  createRedirect({
    fromPath: "/img/projects/strawberryshake-banner.svg",
    toPath: "/resources/strawberryshake-banner.svg",
    redirectInBrowser: true,
    isPermanent: true,
  });
  createRedirect({
    fromPath: "/img/projects/strawberryshake-signet.png",
    toPath: "/resources/strawberryshake-signet.png",
    redirectInBrowser: true,
    isPermanent: true,
  });
};

exports.onCreateNode = async ({ node, actions, getNode, reporter }) => {
  const { createNodeField } = actions;

  if (node.internal.type !== `Mdx`) {
    return;
  }

  // if the path is defined on the frontmatter (like for posts) use that as slug
  let path = node.frontmatter?.path;

  if (!path) {
    path = createFilePath({ node, getNode });

    const parent = getNode(node.parent);

    // if the current file is emitted from the docs directory
    if (parent?.sourceInstanceName === "docs") {
      path = "/docs" + path;
    }

    // remove trailing slashes
    path = path.replace(/\/+$/, "");
  }

  createNodeField({
    name: `slug`,
    node,
    value: path,
  });

  let authorName = "Unknown";
  let lastUpdated = "0000-00-00";

  // we only run "git log" when building the production bundle
  // for development purposes we fallback to dummy values
  if (process.env.NODE_ENV === "production") {
    try {
      const result = await getGitLog(node.fileAbsolutePath);
      const data = result.latest;

      if (data.authorName) {
        authorName = data.authorName;
      }

      if (data.date) {
        lastUpdated = data.date;
      }
    } catch (error) {
      reporter.error(
        `Could not retrieve git information for ${node.fileAbsolutePath}`,
        error
      );
    }
  }

  createNodeField({
    node,
    name: `lastAuthorName`,
    value: authorName,
  });
  createNodeField({
    node,
    name: `lastUpdated`,
    value: lastUpdated,
  });
};

function createBlogArticles(createPage, data) {
  const blogArticleTemplate = path.resolve(
    `src/templates/blog-article-template.tsx`
  );
  const { posts, tags } = data;

  // Create Single Pages
  posts.forEach((post) => {
    createPage({
      path: post.fields.slug,
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

  tags.forEach((tag) => {
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
  const docTemplate = path.resolve(`src/templates/doc-page-template.tsx`);
  const { pages } = data;

  // Create Single Pages
  pages.forEach((page) => {
    const path = page.childMdx.fields.slug;
    const originPath = `${page.relativeDirectory}/${page.name}.md`;

    createPage({
      path,
      component: docTemplate,
      context: {
        originPath,
      },
    });
  });
}

function getGitLog(filepath) {
  const logOptions = {
    file: filepath,
    n: 1,
    format: {
      date: `%cs`,
      authorName: `%an`,
    },
  };

  return git().log(logOptions);
}
