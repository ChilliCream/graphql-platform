function getCacheKey(node) {
  return `gatsby-remark-gather-links-${node.id}-${node.internal.contentDigest}`;
}

module.exports = { getCacheKey };
