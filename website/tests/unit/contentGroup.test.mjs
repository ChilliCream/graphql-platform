import assert from "node:assert/strict";
import test from "node:test";

import { getContentGroup } from "../../src/helpers/contentGroup.ts";

test("getContentGroup maps every section route to its content group", () => {
  // arrange
  const routes = [
    "/",
    "/docs/nitro",
    "/learn",
    "/learn/articles/hot-chocolate-16",
    "/products/nitro",
    "/platform/analytics",
    "/services/support",
    "/pricing",
    "/resources",
    "/help",
    "/legal/privacy-policy",
    "/licensing/chillicream-license",
  ];

  // act
  const groups = routes.map((route) => getContentGroup(route));

  // assert
  assert.deepEqual(groups, [
    "Home",
    "Documentation",
    "Learn",
    "Learn",
    "Products",
    "Platform",
    "Services",
    "Pricing",
    "Resources",
    "Help",
    "Legal",
    "Legal",
  ]);
});

test("getContentGroup falls back to Other for unmapped routes", () => {
  // act
  const groups = ["/blog", "/blog/rss.xml", "/tv", "/whatever"].map((route) => getContentGroup(route));

  // assert
  assert.deepEqual(groups, ["Other", "Other", "Other", "Other"]);
});
