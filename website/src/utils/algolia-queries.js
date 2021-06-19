const blogQuery = `
  {
    blog: allMdx(
      limit: 1000
      filter: { frontmatter: { path: { regex: "//blog(/.*)?/" } } }
      sort: { order: DESC, fields: [frontmatter___date] }
    ) {
      nodes {
        id
        fields {
          slug
        }
        excerpt(pruneLength: 100000)
      }
    }
  }
`;

const docsQuery = `
  {
    docs: allFile(
      limit: 1000
      filter: { sourceInstanceName: { eq: "docs" }, extension: { eq: "md" } }
    ) {
      nodes {
        childMdx {
          id
          fields {
            slug
          }
          excerpt(pruneLength: 100000)
        }
      }
    }
  }
`;

function pageToAlgoliaRecord({ id, frontmatter, fields, ...rest }) {
  return {
    objectID: id,
    ...frontmatter,
    ...fields,
    ...rest,
  };
}

const querySearchSetting = {
  attributesToSnippet: [`excerpt:5`],
  attributesToRetrieve: [`slug`],
};

const index = process.env.GATSBY_ALGOLIA_INDEX;

const queries = [
  {
    query: blogQuery,
    transformer: ({ data }) => data.blog.nodes.map(pageToAlgoliaRecord),
    indexName: index,
    settings: querySearchSetting,
  },
  {
    query: docsQuery,
    transformer: ({ data }) =>
      data.docs.nodes.map((node) => pageToAlgoliaRecord(node.childMdx)),
    indexName: index,
    settings: querySearchSetting,
  },
];

module.exports = queries;
