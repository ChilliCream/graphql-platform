// this is mostly taken from
// https://github.com/trevorblades/gatsby-remark-check-links

const visit = require("unist-util-visit");

// todo: non-inline links are not correctly handled
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

  const headingAnchors = [];
  const links = [];

  // collect all links and headings from the current file
  visit(markdownAST, "link", (node, _, parent) => {
    // if this is the little link anchor next to a heading
    // note: this method of header discovery assumes that every header
    //       is covered by gatsby-remark-autolink-headers
    if (parent.type === "heading") {
      // the id is the generated link for this heading
      headingAnchors.push(parent.data.id);
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

  cache.set(getCacheKey(parent), {
    slug: markdownNode.fields.slug,
    links,
    headingAnchors: headingAnchors,
    absolutePath: parent.absolutePath,
  });

  // todo: ideally we would have a lifecycle method that runs after
  // all of this is done, so we don't have to loop over all files for each file

  // local cache of all markdown documents
  const documentMap = {};

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
      documentMap[cacheEntry.slug] = cacheEntry;

      continue;
    }

    // don't continue if a page hasn't been visited yet
    return markdownAST;
  }

  // todo: the below part is run twice

  let totalBrokenLinks = 0;

  // iterate over all documents
  for (const documentSlug in documentMap) {
    const document = documentMap[documentSlug];

    // document contains no links
    if (!document || document.links.length < 1) {
      continue;
    }

    // iterate over all links of the current document
    const brokenLinks = document.links.filter((link) => {
      // extract potential heading from link
      const { link: key, hash: headingAnchor } = deconstructLink(
        link.url,
        documentSlug
      );

      const linkedDoc = documentMap[key];

      // no document with the used slug was found --> dead link
      if (!linkedDoc) {
        return true;
      }

      // does the linked document contain the heading
      if (headingAnchor) {
        return !linkedDoc.headingAnchors.includes(headingAnchor);
      }

      return false;
    });

    if (brokenLinks.length > 0) {
      const filepath = document.absolutePath;

      console.warn(
        `${brokenLinks.length} broken link(s) found in ${documentSlug}`
      );

      // output links to location of broken links
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

        console.warn(`- ${link.url} ( ${filepath}${lineColumn} )`);
      }

      console.log("");
    }

    totalBrokenLinks += brokenLinks.length;
  }

  if (totalBrokenLinks > 0) {
    const message = `${totalBrokenLinks} broken link(s) found`;

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

function deconstructLink(targetLink, documentSlug) {
  let link = targetLink;
  let hash = null;

  const hashIndex = targetLink.indexOf("#");

  if (hashIndex > -1) {
    // link to heading within same document
    if (hashIndex === 0) {
      link = documentSlug;
      // link to heading in (potentially) another document
    } else {
      link = targetLink.slice(0, hashIndex);
    }

    hash = targetLink.slice(hashIndex + 1);
  }

  return {
    link,
    hash,
  };
}
