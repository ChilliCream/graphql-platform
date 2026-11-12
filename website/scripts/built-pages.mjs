import fs from "node:fs/promises";
import path from "node:path";

const NOT_FOUND_ROUTE = /^(?:\/_not-found|(?:\/.*)?\/404)$/;

export function routeForBuiltHtml(relativePath) {
  const posix = relativePath.split(path.sep).join("/");
  const route = `/${posix.replace(/\.html$/, "")}`;
  if (route === "/index") {
    return "/";
  }
  return route.endsWith("/index") ? route.slice(0, -"/index".length) : route;
}

export async function listBuiltRoutes(outputRoot) {
  const routes = [];
  const pending = [outputRoot];
  while (pending.length > 0) {
    const directory = pending.pop();
    for (const entry of await fs.readdir(directory, { withFileTypes: true })) {
      const fullPath = path.join(directory, entry.name);
      if (entry.isDirectory()) {
        pending.push(fullPath);
      } else if (entry.isFile() && entry.name.endsWith(".html")) {
        routes.push(routeForBuiltHtml(path.relative(outputRoot, fullPath)));
      }
    }
  }
  return routes.sort();
}

export function isNotFoundRoute(route) {
  return NOT_FOUND_ROUTE.test(route);
}

export function findUncoveredRoutes(routes, { crawled, isExcluded }) {
  return routes.filter((route) => !crawled.has(route) && !isExcluded(route));
}
