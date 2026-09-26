import { SecurityCheckpoint } from "./security/SecurityCheckpoint";

/**
 * Entry point for the Security row's graphic; kept as its own file and
 * export so the row it composes into never has to change. The graphic
 * itself — an animated policy checkpoint at the router — lives in
 * `./security`.
 */
export function SecurityWindow() {
  return <SecurityCheckpoint />;
}
