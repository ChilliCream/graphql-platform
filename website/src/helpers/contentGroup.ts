/**
 * Maps a route to the GA4 content group reported for the page view, so reports
 * can be broken down by section instead of by individual URL. Routes that are
 * not part of a named section fall back to "Other".
 */
export function getContentGroup(pathname: string): string {
  if (pathname.startsWith("/docs")) {
    return "Documentation";
  }
  if (pathname.startsWith("/learn")) {
    return "Learn";
  }
  if (pathname.startsWith("/products")) {
    return "Products";
  }
  if (pathname.startsWith("/platform")) {
    return "Platform";
  }
  if (pathname.startsWith("/services")) {
    return "Services";
  }
  if (pathname.startsWith("/pricing")) {
    return "Pricing";
  }
  if (pathname.startsWith("/resources")) {
    return "Resources";
  }
  if (pathname.startsWith("/help")) {
    return "Help";
  }
  if (pathname.startsWith("/legal") || pathname.startsWith("/licensing")) {
    return "Legal";
  }
  if (pathname === "/") {
    return "Home";
  }
  return "Other";
}
