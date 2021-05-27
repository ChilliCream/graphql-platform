// this is mostly taken from
// https://github.com/trevorblades/gatsby-remark-check-links

const visit = require("unist-util-visit");

module.exports = async function plugin({
  markdownAST,
  markdownNode,
  cache,
  getCache,
  getNode,
  files,
}) {
  // if there's no slug, it is not a markdown file we're interested in
  if (!markdownNode.fields?.slug) {
    return markdownAST;
  }

  const headingSlugs = [];
  const links = [];

  // collect all links and headings from the current file
  visit(markdownAST, "link", (node, _, parent) => {
    // if this is the little link anchor next to a heading
    // note: this method of header discovery assumes that every header
    //       is covered by gatsby-remark-autolink-headers
    if (parent.type === "heading") {
      // the id is the generated slug for this heading
      headingSlugs.push(parent.data.id);
      return;
    }

    const isHttpLink = /^https?\:\/\//.test(node.url);

    // if this is a link that does not point to one of our documents
    if (isHttpLink) {
      return;
    }

    links.push({
      position: node.position,
      url: node.url,
      frontmatter: markdownNode.frontmatter,
    });
  });

  const parent = await getNode(markdownNode.parent);

  let path = markdownNode.fields.slug;
  if (parent.sourceInstanceName) {
    path = "/" + parent.sourceInstanceName + path;
  }

  // remove trailing slashes
  path = path.replace(/\/+$/, "");

  cache.set(getCacheKey(parent), {
    path,
    links,
    headingSlugs,
  });

  // lookup for heading slugs per document path
  const headingSlugMap = {};
  // lookup for links per document path
  const linkMap = {};

  // check if all other files have entries in the cache
  for (let index = 0; index < files.length; index++) {
    const file = files[index];
    // ignore non mdx files
    if (!/^mdx?$/.test(file.extension)) {
      continue;
    }

    const cacheKey = getCacheKey(file);

    let cacheEntry = await cache.get(cacheKey);
    if (!cacheEntry && getCache) {
      // the cache provided to `gatsby-mdx` has its own namespace, and it
      // doesn't have access to `getCache`, so we have to check to see if
      // those files have been visited here.
      const mdxCache = getCache("gatsby-plugin-mdx");
      cacheEntry = await mdxCache.get(cacheKey);
    }

    if (cacheEntry) {
      headingSlugMap[cacheEntry.path] = cacheEntry.headingSlugs;
      linkMap[cacheEntry.path] = cacheEntry.links;

      continue;
    }

    // don't continue if a page hasn't been visited yet
    return;
  }

  let totalBrokenLinks = 0;

  for (const path in linkMap) {
    const linksInDocument = linkMap[path];

    if (!linksInDocument || linksInDocument.length < 1) {
      continue;
    }

    const brokenLinks = linksInDocument.filter((link) => {
      const { key, hasHash, hashIndex } = getHeadingsMapKey(link.url, path);

      const headingsSlugsInDocument = headingSlugMap[key];

      if (!headingsSlugsInDocument) {
        return true;
      }

      if (headingsSlugsInDocument.length < 1) {
        return false;
      }

      if (hasHash) {
        const slug = link.url.slice(hashIndex + 1);

        return !headingsSlugsInDocument.includes(slug);
      }

      return false;
    });

    if (brokenLinks.length > 0) {
      // todo: this won't work with index.md files
      const filepath = `./src${path}.md`;

      console.warn(`${brokenLinks.length} broken link(s) found in ${filepath}`);

      for (const link of brokenLinks) {
        let lineColumn = "";

        if (link.position) {
          const { line, column } = link.position.start;

          // account for the offset that frontmatter adds
          const offset = link.frontmatter
            ? Object.keys(link.frontmatter).length + 2
            : 0;

          lineColumn = `:${line + offset}:${column}`;
        }

        console.warn(`- ${link.url} [${filepath}${lineColumn}]`);
      }

      console.log("");
    }

    totalBrokenLinks += brokenLinks.length;
  }

  if (totalBrokenLinks > 0) {
    const message = `${totalBrokenLinks} broken links found`;

    if (process.env.NODE_ENV === "production") {
      throw new Error(message);
    } else {
      console.warn(message);
    }
  }

  return markdownAST;
};

function getCacheKey(node) {
  return `detect-broken-links-${node.id}-${node.internal.contentDigest}`;
}

function getHeadingsMapKey(link, path) {
  let key = link;
  const hashIndex = link.indexOf("#");
  const hasHash = hashIndex !== -1;
  if (hasHash) {
    key = link.startsWith("#") ? path : link.slice(0, hashIndex);
  }

  return {
    key,
    hasHash,
    hashIndex,
  };
}
