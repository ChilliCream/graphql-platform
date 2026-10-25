import * as React from "react";

/**
 * next/og renders React components through Satori, which (unlike React DOM and
 * the server renderer) never installs a hook dispatcher. Any component in a
 * share card that calls a hook therefore crashes with
 * "Cannot read properties of null (reading 'useId')" during prerender. The only
 * hook our cards rely on is `useId` (icons derive unique gradient ids from it),
 * so we install a minimal dispatcher that implements just that.
 *
 * Satori renders each element once and the output is never hydrated, so the ids
 * only need to be unique within a single card, not stable across environments.
 */
type SatoriDispatcher = {
  readonly useId: () => string;
};

// React keeps the active hook dispatcher on the `H` field of its shared
// internals object, which is `null` whenever React itself is not mid-render
// (exactly the case while Satori walks the tree). The internals live under a
// different export depending on which build is loaded: OG routes run under the
// `react-server` condition (the SERVER build), while the CLIENT build is used
// elsewhere.
type ReactInternals = { H: SatoriDispatcher | null };
const reactWithInternals = React as unknown as {
  __SERVER_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE?: ReactInternals;
  __CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE?: ReactInternals;
};
const internals =
  reactWithInternals.__SERVER_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE ??
  reactWithInternals.__CLIENT_INTERNALS_DO_NOT_USE_OR_WARN_USERS_THEY_CANNOT_UPGRADE;

let idCounter = 0;

const satoriDispatcher: SatoriDispatcher = {
  useId: () => `:satori-${(idCounter += 1)}:`,
};

/**
 * Makes `useId` usable while Satori renders an OG image. Call this before
 * constructing an `ImageResponse`. It only fills the dispatcher when none is
 * active (`H === null`), so it never clobbers a real React render in progress
 * and is safe to call repeatedly.
 */
export function enableSatoriHooks(): void {
  if (internals && internals.H === null) {
    internals.H = satoriDispatcher;
  }
}
