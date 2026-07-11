declare module "@/src/remark/*.mjs" {
  import type { Plugin } from "unified";
  const plugin: Plugin;
  export default plugin;
}

declare module "@/src/recma/*.mjs" {
  import type { Plugin } from "unified";
  const plugin: Plugin;
  export default plugin;
}
