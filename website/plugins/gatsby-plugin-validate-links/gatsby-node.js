const {
  getDocumentsCache,
  getBrokenLinks,
  displayBrokenLinks,
} = require("./utils");

exports.onPreBuild = async ({
  getNodesByType,
  getNode,
  cache,
  getCache,
  reporter,
}) => {
  const activity = reporter.activityTimer(`validate markdown links`, {
    id: `validate-markdown-links`,
  });

  activity.start();

  const mdxFiles = getNodesByType("Mdx").map((node) => getNode(node.parent));

  if (mdxFiles.some((file) => !file)) {
    activity.panicOnBuild("MDX nodes without a parent encountered");
  }

  const documents = await getDocumentsCache(mdxFiles, cache, getCache);

  if (!documents) {
    activity.panicOnBuild("Document cache failed to load");
  }

  // Unversioned pages are currently created in a special way and only to be
  // backwards compatible. All but the root links should be versioned anyways
  // so the link validation will not throw errors.
  // We hardcode the root documents here to satisfy the link validation,
  // if pages refer to the unversioned root page of a product.
  const hardcodedPages = [
    "/docs/hotchocolate",
    "/docs/strawberryshake",
    "/docs/bananacakepop",
  ];

  hardcodedPages.forEach((hardcodedPage) => {
    documents[hardcodedPage] = {
      slug: hardcodedPage,
      links: [],
      headingAnchors: [],
    };
  });

  let totalBrokenLinks = 0;

  for (const key in documents) {
    const document = documents[key];

    // document contains no links
    if (!document || document.links.length < 1) {
      continue;
    }

    const brokenLinks = getBrokenLinks(document, documents);

    if (brokenLinks.length > 0) {
      console.log("");

      console.warn(
        `${brokenLinks.length} broken link(s) found in ${document.slug}`
      );

      const filepath = document.absolutePath;

      displayBrokenLinks(brokenLinks, filepath);
    }

    totalBrokenLinks += brokenLinks.length;
  }

  if (totalBrokenLinks > 0) {
    activity.panicOnBuild(`${totalBrokenLinks} broken link(s) found`);
  }

  activity.end();
};
