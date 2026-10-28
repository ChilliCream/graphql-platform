import { createReadStream, existsSync, statSync } from "node:fs";
import { createServer } from "node:http";
import { extname, join, normalize, resolve } from "node:path";

const root = resolve(process.argv[2] ?? "out");
const port = Number(process.argv[3]);

const types = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".txt": "text/plain; charset=utf-8",
  ".md": "text/markdown; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".webp": "image/webp",
  ".avif": "image/avif",
  ".ico": "image/x-icon",
  ".woff2": "font/woff2",
  ".webmanifest": "application/manifest+json",
};

function isFile(path) {
  return existsSync(path) && statSync(path).isFile();
}

// Resolves a request path like the production nginx: the exact file, then the
// extensionless page (`/pricing` to `pricing.html`), then a directory index.
function locate(pathname) {
  const target = join(root, normalize(decodeURIComponent(pathname)));
  if (!target.startsWith(root)) {
    return undefined;
  }
  return [target, `${target}.html`, join(target, "index.html")].find(isFile);
}

createServer((request, response) => {
  const { pathname } = new URL(request.url ?? "/", "http://localhost");
  const file = locate(pathname);
  const served = file ?? join(root, "404.html");
  response.writeHead(file ? 200 : 404, {
    "content-type": types[extname(served)] ?? "application/octet-stream",
    "cache-control": "no-store",
  });
  createReadStream(served).pipe(response);
}).listen(port, "127.0.0.1");
