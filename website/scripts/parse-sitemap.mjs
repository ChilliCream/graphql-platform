import { parse } from "node-html-parser";

export function parseSitemapUrls(xml) {
  const document = parse(xml);

  return document
    .querySelectorAll("loc")
    .map((location) => location.textContent.trim());
}
