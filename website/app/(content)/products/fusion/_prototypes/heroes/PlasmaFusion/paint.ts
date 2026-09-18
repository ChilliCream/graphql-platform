import { BRAND } from "../../../tokens";
import { hexToRgba, mixHexToRgba, WARM_TINT_MAX, whiteRgba } from "./colors";
import type { FilamentPath } from "./filaments";
import type { PlasmaLayout } from "./layout";

const SPHERES = ["sphereA", "sphereB"] as const;

function strokeFilament(
  ctx: CanvasRenderingContext2D,
  path: FilamentPath,
  strokeStyle: string,
  lineWidth: number,
): void {
  if (path.points.length < 2) {
    return;
  }
  ctx.beginPath();
  ctx.moveTo(path.points[0].x, path.points[0].y);
  for (let i = 1; i < path.points.length; i++) {
    ctx.lineTo(path.points[i].x, path.points[i].y);
  }
  ctx.strokeStyle = strokeStyle;
  ctx.lineWidth = lineWidth;
  ctx.stroke();
}

/**
 * Static structure: the two sphere shells (a soft ring-shaped glow, never a
 * stroked edge), the low-alpha floor glow, the dense static majority of the
 * filament web, the faint beam base and the vignette. Painted once on mount
 * and again on resize (`measure()`); the animated layer draws a small live
 * subset of filaments, the core flare and the beam shimmer on top of this
 * every frame instead of rebuilding any of it.
 */
export function paintStatic(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  layout: PlasmaLayout,
  staticPathsA: readonly FilamentPath[],
  staticPathsB: readonly FilamentPath[],
): void {
  ctx.clearRect(0, 0, w, h);
  ctx.globalCompositeOperation = "source-over";

  // Low-alpha floor glow so the desktop scene's lower quarter reads as
  // static light instead of bare navy, without adding a new shape (planner
  // craft read, ticket hc-0-wrc.2 comment 29/31, minor C4).
  if (!layout.mobile) {
    const floorRadius = layout.radius * 2.2;
    const floor = ctx.createRadialGradient(
      layout.core.x,
      layout.core.y,
      0,
      layout.core.x,
      layout.core.y,
      floorRadius,
    );
    floor.addColorStop(0, hexToRgba(BRAND.cyan, 0.07));
    floor.addColorStop(0.6, hexToRgba(BRAND.cyan, 0.03));
    floor.addColorStop(1, hexToRgba(BRAND.cyan, 0));
    ctx.fillStyle = floor;
    ctx.fillRect(0, 0, w, h);
  }

  // Sphere shells: a soft ring-shaped radial gradient peaking just inside
  // the radius (about 0.9-0.95x) and falling off both ways to 0 -- never a
  // stroked rim. A large shape's silhouette must come from filament density
  // plus this glow, not a hard vector edge (README section 4; a visible rim
  // is a major finding on its own, comment 29 item 1).
  for (const key of SPHERES) {
    const center = layout[key];
    const outer = layout.radius * 1.15;
    const stop = (fractionOfRadius: number) =>
      (fractionOfRadius * layout.radius) / outer;
    const shell = ctx.createRadialGradient(
      center.x,
      center.y,
      0,
      center.x,
      center.y,
      outer,
    );
    shell.addColorStop(0, hexToRgba(BRAND.cyan, 0));
    shell.addColorStop(stop(0.55), hexToRgba(BRAND.cyan, 0.02));
    shell.addColorStop(stop(0.9), hexToRgba(BRAND.cyan, 0.1));
    shell.addColorStop(stop(0.95), hexToRgba(BRAND.cyan, 0.12));
    shell.addColorStop(stop(1.0), hexToRgba(BRAND.cyan, 0.06));
    shell.addColorStop(1, hexToRgba(BRAND.cyan, 0));
    ctx.fillStyle = shell;
    ctx.beginPath();
    ctx.arc(center.x, center.y, outer, 0, Math.PI * 2);
    ctx.fill();
  }

  // The dense majority of the filament web is baked in here once (comment 29
  // item 2: 3-5x the strand count, a wide soft cyan glow under 1-1.5px
  // near-white centres); a small live subset drawn every frame on the
  // animated layer keeps the crawl/reseed motion without repainting this.
  ctx.lineCap = "round";
  const allStaticPaths = [...staticPathsA, ...staticPathsB];
  ctx.globalCompositeOperation = "lighter";
  for (const path of allStaticPaths) {
    strokeFilament(
      ctx,
      path,
      mixHexToRgba(
        BRAND.cyan,
        BRAND.coral,
        path.warm * WARM_TINT_MAX,
        path.alpha * 0.4,
      ),
      path.width * 3,
    );
  }
  ctx.globalCompositeOperation = "source-over";
  for (const path of allStaticPaths) {
    strokeFilament(ctx, path, whiteRgba(path.alpha * 0.8), path.width);
  }

  // Feather the shells/web out of the copy-clear zone: this only cleans up
  // stray pixels at the zone edge (`artLeft`/`artTop`) -- the shells and web
  // themselves are already centred on the ruled sphere geometry, so no
  // erased-interior ring can appear here (planner ruling 2, ticket
  // hc-0-wrc.2 comment 152).
  ctx.globalCompositeOperation = "destination-out";
  if (!layout.mobile) {
    const featherLeft = layout.artLeft;
    const featherRight = layout.artLeft + 24;
    const feather = ctx.createLinearGradient(featherLeft, 0, featherRight, 0);
    feather.addColorStop(0, "rgba(0,0,0,1)");
    feather.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = feather;
    ctx.fillRect(0, 0, w, h);
  } else {
    const featherTop = layout.artTop;
    const featherBottom = layout.artTop + 24;
    const feather = ctx.createLinearGradient(0, featherTop, 0, featherBottom);
    feather.addColorStop(0, "rgba(0,0,0,1)");
    feather.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = feather;
    ctx.fillRect(0, 0, w, h);
  }
  ctx.globalCompositeOperation = "source-over";

  // Faint full-width beam base, drawn after the feather mask so it still
  // spans the whole width; the animated layer adds the shimmer + the bright
  // core-coloured centre on top every frame. Brighter on mobile so it reads
  // through the denser mobile knot.
  const span = Math.max(w, 1);
  const cx = layout.core.x / span;
  const beamCenterAlpha = layout.mobile ? 0.3 : 0.16;
  const beamSideAlpha = layout.mobile ? 0.12 : 0.05;
  const beam = ctx.createLinearGradient(0, 0, span, 0);
  beam.addColorStop(0, hexToRgba(BRAND.cyan, 0));
  beam.addColorStop(
    Math.max(0, cx - 0.35),
    hexToRgba(BRAND.cyan, beamSideAlpha),
  );
  beam.addColorStop(cx, hexToRgba(BRAND.cyan, beamCenterAlpha));
  beam.addColorStop(
    Math.min(1, cx + 0.35),
    hexToRgba(BRAND.cyan, beamSideAlpha),
  );
  beam.addColorStop(1, hexToRgba(BRAND.cyan, 0));
  ctx.fillStyle = beam;
  ctx.fillRect(0, layout.beamY - 1, w, 2);

  const vignette = ctx.createRadialGradient(
    w / 2,
    h / 2,
    Math.min(w, h) * 0.32,
    w / 2,
    h / 2,
    Math.max(w, h) * 0.75,
  );
  vignette.addColorStop(0, hexToRgba(BRAND.navy, 0));
  vignette.addColorStop(1, hexToRgba(BRAND.navy, 0.5));
  ctx.fillStyle = vignette;
  ctx.fillRect(0, 0, w, h);
}
