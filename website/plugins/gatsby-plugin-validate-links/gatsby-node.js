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

  const knownLinks = [
    "/docs/bananacakepop/v2/apis/client-registry",
    "/products/bananacakepop",
    "/products/hotchocolate",
    "/products/nitro",
    "/products/strawberryshake",
  ];

  knownLinks.forEach((knownLink) => {
    documents[knownLink] = {
      slug: knownLink,
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
