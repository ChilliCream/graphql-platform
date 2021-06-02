const {
  getDocumentsCache,
  getBrokenLinks,
  displayBrokenLinks,
} = require("./utils");

exports.onPostBuild = async ({ getNodesByType, getNode, cache, getCache }) => {
  console.log("VALIDATE LINKS");

  const files = getNodesByType("Mdx").map((node) => getNode(node.parent));

  if (files.some((file) => !file)) {
    console.warn("MDX nodes without a parent encountered");

    return;
  }

  const documents = await getDocumentsCache(files, cache, getCache);

  if (!documents) {
    console.warn("Document cache failed to load");

    return;
  }

  let totalBrokenLinks = 0;

  for (const key in documents) {
    const document = documents[key];

    // document contains no links
    if (!document || document.links.length < 1) {
      continue;
    }

    const brokenLinks = getBrokenLinks(document, documents);

    if (brokenLinks.length > 0) {
      console.warn(
        `${brokenLinks.length} broken link(s) found in ${document.slug}`
      );

      const filepath = document.absolutePath;

      displayBrokenLinks(brokenLinks, filepath);

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
};
