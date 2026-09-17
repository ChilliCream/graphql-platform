import { BRAND } from "../../../tokens";
import { hexToRgba } from "./colors";
import type { PlasmaLayout } from "./layout";

const SPHERES = ["sphereA", "sphereB"] as const;

/**
 * Static structure: the two sphere shells (a faint radial gradient plus a
 * rim so the silhouette reads even between filaments), the faint beam base
 * and the vignette. Painted once on mount and again on resize; the animated
 * layer draws the filaments, the core flare and the beam shimmer on top of
 * this every frame instead of rebuilding any of it.
 */
export function paintStatic(
  ctx: CanvasRenderingContext2D,
  w: number,
  h: number,
  layout: PlasmaLayout,
): void {
  ctx.clearRect(0, 0, w, h);
  ctx.globalCompositeOperation = "source-over";

  for (const key of SPHERES) {
    const center = layout[key];
    const shell = ctx.createRadialGradient(
      center.x,
      center.y,
      layout.radius * 0.1,
      center.x,
      center.y,
      layout.radius * 1.08,
    );
    shell.addColorStop(0, hexToRgba(BRAND.cyan, 0.09));
    shell.addColorStop(0.55, hexToRgba(BRAND.cyan, 0.045));
    shell.addColorStop(1, hexToRgba(BRAND.cyan, 0));
    ctx.fillStyle = shell;
    ctx.beginPath();
    ctx.arc(center.x, center.y, layout.radius * 1.08, 0, Math.PI * 2);
    ctx.fill();

    ctx.strokeStyle = hexToRgba(BRAND.cyan, 0.1);
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.arc(center.x, center.y, layout.radius, 0, Math.PI * 2);
    ctx.stroke();
  }

  // Feather the shells/rims out of the copy-clear zone: this only cleans up
  // stray pixels at the zone edge (`artLeft`/`artTop`) -- the shells and
  // rims themselves are already centred on the ruled sphere geometry, so no
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
