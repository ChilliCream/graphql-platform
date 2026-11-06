import assert from "node:assert/strict";
import test from "node:test";
import { parse } from "node-html-parser";
import {
  findUncoveredRoutes,
  isNotFoundRoute,
  routeForBuiltHtml,
} from "./built-pages.mjs";
import {
  headingSkips,
  imagesWithoutAlt,
  lengthWarnings,
  openGraphErrors,
} from "./seo-rules.mjs";
import {
  createReport,
  formatGroups,
  formatSummary,
  printReport,
} from "./validation-report.mjs";

test("routeForBuiltHtml maps built files to routes", () => {
  assert.equal(routeForBuiltHtml("index.html"), "/");
  assert.equal(routeForBuiltHtml("pricing.html"), "/pricing");
  assert.equal(routeForBuiltHtml("docs/fusion/index.html"), "/docs/fusion");
  assert.equal(
    routeForBuiltHtml("blog/tags/graphql/2.html"),
    "/blog/tags/graphql/2",
  );
});

test("isNotFoundRoute matches framework and section 404 pages only", () => {
  assert.deepEqual(
    ["/404", "/_not-found", "/docs/nitro/404", "/docs/404-errors"].map(
      isNotFoundRoute,
    ),
    [true, true, true, false],
  );
});

test("findUncoveredRoutes returns routes neither crawled nor excluded", () => {
  const uncovered = findUncoveredRoutes(["/", "/a", "/b", "/c"], {
    crawled: new Set(["/", "/a"]),
    isExcluded: (route) => route === "/b",
  });
  assert.deepEqual(uncovered, ["/c"]);
});

test("lengthWarnings flags values outside the allowed ranges", () => {
  const warnings = lengthWarnings("/x", "Too short", "d".repeat(171));
  assert.deepEqual(warnings, [
    ["seo-title-length", "/x: title is 9 characters, expected 15-70"],
    [
      "seo-description-length",
      "/x: description is 171 characters, expected 50-170",
    ],
  ]);
  assert.deepEqual(lengthWarnings("/x", "t".repeat(15), "d".repeat(50)), []);
});

test("openGraphErrors reports every missing or relative tag", () => {
  const document = parse('<meta property="og:image" content="/relative.png">');
  assert.deepEqual(openGraphErrors("/x", document), [
    ["og-title-missing", "/x: missing og:title"],
    ["og-description-missing", "/x: missing og:description"],
    [
      "og-image-not-absolute",
      "/x: og:image is not an absolute URL (/relative.png)",
    ],
    ["twitter-card-missing", "/x: missing twitter:card"],
  ]);
});

test("openGraphErrors accepts a complete tag set", () => {
  const document = parse(`
    <meta property="og:title" content="t">
    <meta property="og:description" content="d">
    <meta property="og:image" content="https://example.com/i.png">
    <meta name="twitter:card" content="summary_large_image">`);
  assert.deepEqual(openGraphErrors("/x", document), []);
});

test("headingSkips reports increases larger than one level", () => {
  const document = parse("<h1>a</h1><h3>b</h3><h2>c</h2><h4>d</h4>");
  assert.deepEqual(headingSkips(document), [
    "h1 followed by h3",
    "h2 followed by h4",
  ]);
});

test("imagesWithoutAlt counts images lacking the attribute only", () => {
  const document = parse('<img src="a"><img src="b" alt=""><img alt="c">');
  assert.equal(imagesWithoutAlt(document), 1);
});

test("report groups findings by rule with counts and a summary", () => {
  const report = createReport();
  report.error("b-rule", "/one: broken");
  report.error("a-rule", "/two: broken");
  report.error("a-rule", "/three: broken");
  report.warn("w-rule", "/four: odd");

  assert.deepEqual(formatGroups(report.findings, "error"), [
    "[a-rule] 2",
    "  - /two: broken",
    "  - /three: broken",
    "[b-rule] 1",
    "  - /one: broken",
  ]);
  assert.equal(
    formatSummary(report.findings),
    "Summary: 3 errors in 2 rules, 1 warnings in 1 rules.",
  );
  assert.equal(report.hasErrors(), true);
});

test("formatGroups truncates a group beyond the limit", () => {
  const report = createReport();
  for (let index = 0; index < 3; index++) {
    report.warn("w-rule", `/${index}: odd`);
  }
  assert.deepEqual(formatGroups(report.findings, "warning", 1), [
    "[w-rule] 3",
    "  - /0: odd",
    "  ... and 2 more",
  ]);
});

test("printReport omits the error section when there are only warnings", () => {
  const report = createReport();
  report.warn("w-rule", "/x: odd");
  const lines = [];
  printReport(report, { log: (line) => lines.push(line) });
  assert.deepEqual(lines, [
    "Warnings (1):",
    "[w-rule] 1",
    "  - /x: odd",
    "Summary: 0 errors in 0 rules, 1 warnings in 1 rules.",
  ]);
  assert.equal(report.hasErrors(), false);
});
