import assert from "node:assert/strict";
import test from "node:test";

import {
  ANALYTICS_EVENTS,
  getTrackParams,
  isAnalyticsEventName,
  trackAttributes,
} from "../../src/helpers/analyticsEvents.ts";

test("trackAttributes writes the event name and hyphenates every parameter", () => {
  // arrange
  const params = {
    repo_url: "https://github.com/ChilliCream/graphql-platform",
    item_type: "example",
    item_slug: "todo",
  };

  // act
  const attributes = trackAttributes({ name: "repo_click", params });

  // assert
  assert.deepEqual(attributes, {
    "data-track": "repo_click",
    "data-track-repo-url": "https://github.com/ChilliCream/graphql-platform",
    "data-track-item-type": "example",
    "data-track-item-slug": "todo",
  });
});

test("getTrackParams keeps only data-track-* attributes and converts them to snake_case", () => {
  // act
  const params = getTrackParams([
    { name: "href", value: "/pricing" },
    { name: "data-track", value: "pricing_cta_click" },
    { name: "data-track-plan", value: "dedicated" },
    { name: "data-track-item-slug", value: "hot-chocolate" },
    { name: "data-track-", value: "ignored" },
    { name: "class", value: "btn" },
  ]);

  // assert
  assert.deepEqual(params, { plan: "dedicated", item_slug: "hot-chocolate" });
});

test("getTrackParams returns an empty object when nothing is tracked", () => {
  // act
  const params = getTrackParams([{ name: "href", value: "/pricing" }]);

  // assert
  assert.deepEqual(params, {});
});

test("trackAttributes and getTrackParams round-trip every declared parameter", () => {
  for (const [name, parameterNames] of Object.entries(ANALYTICS_EVENTS)) {
    // arrange
    const params = Object.fromEntries(parameterNames.map((parameter) => [parameter, `value-${parameter}`]));

    // act
    const attributes = trackAttributes({ name, params });
    const roundTripped = getTrackParams(Object.entries(attributes).map(([key, value]) => ({ name: key, value })));

    // assert
    assert.deepEqual(roundTripped, params, `round-trip failed for ${name}`);
  }
});

test("isAnalyticsEventName accepts declared events and rejects everything else", () => {
  // assert
  assert.deepEqual(
    ["contact_form_submit", "nitro_download", "made_up_event", "", undefined].map((value) =>
      isAnalyticsEventName(value),
    ),
    [true, true, false, false, false],
  );
});
