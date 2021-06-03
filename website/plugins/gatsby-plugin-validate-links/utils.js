const { getCacheKey } = require("../shared");

async function getDocumentsCache(files, cache, getCache) {
  const documents = {};

  for (let index = 0; index < files.length; index++) {
    const file = files[index];

    const cacheKey = getCacheKey(file);

    let cacheEntry = await cache.get(cacheKey);
    if (!cacheEntry && getCache) {
      // the cache provided to `gatsby-mdx` has its own namespace, and it
      // doesn't have access to `getCache`, so we have to check to see if
      // those files have been visited here.
      const mdxCache = getCache("gatsby-plugin-mdx");
      cacheEntry = await mdxCache.get(cacheKey);
    }

    if (!cacheEntry) {
      return null;
    }

    documents[cacheEntry.slug] = cacheEntry;
  }

  return documents;
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

function getBrokenLinks(document, otherDocuments) {
  return document.links.filter((link) => {
    // extract potential heading from link
    const { link: key, hash: headingAnchor } = deconstructLink(
      link.url,
      document.slug
    );

    const linkedDoc = otherDocuments[key];

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
}

function displayBrokenLinks(brokenLinks, filepath) {
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

    console.warn(`${filepath}${lineColumn} ("${link.url}")`);
  }
}

module.exports = {
  getDocumentsCache,
  getBrokenLinks,
  displayBrokenLinks,
};
