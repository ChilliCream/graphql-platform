// Auto-generated raw-WebGL1 hero shaders (ChilliCream brand palette on navy #0b0f1a).
// Uniforms: u_res (vec2), u_time (float).

export const HERO_VERTEX = `attribute vec2 a_pos;
void main(){ gl_Position = vec4(a_pos, 0.0, 1.0); }`;

export const SUNRISE_FRAG = `precision highp float;
uniform vec2 u_res;
uniform float u_time;

float hash21(vec2 p){
  vec3 p3 = fract(vec3(p.xyx) * 0.1031);
  p3 += dot(p3, p3.yzx + 33.33);
  return fract((p3.x + p3.y) * p3.z);
}
vec2 hash22(vec2 p){
  vec3 p3 = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
  p3 += dot(p3, p3.yzx + 33.33);
  return fract((p3.xx + p3.yz) * p3.zy);
}

void main(){
  vec2 uv = gl_FragCoord.xy / u_res;
  float aspect = u_res.x / u_res.y;
  vec2 p = uv - 0.5;
  p.x *= aspect;

  // --- geometry: large rising circle, only top rim visible ---
  float cy = -2.35;
  float R  = 2.63;
  vec2  ctr = vec2(0.0, cy);
  vec2  cp  = vec2(0.0, cy + R);      // crest point (top of circle)
  vec2  pp  = p - ctr;
  float d   = length(pp);
  vec2  dir = pp / d;
  float up  = clamp(dir.y, 0.0, 1.0); // 1 at crest, 0 at sides

  float sd = d - R;                   // signed distance to rim
  float outside = smoothstep(-0.05, 0.015, sd);

  // --- base background: brand navy #0b0f1a ---
  vec3 top = vec3(0.030, 0.044, 0.078);
  vec3 bot = vec3(0.043, 0.059, 0.102);   // navy #0b0f1a
  vec3 col = mix(bot, top, uv.y);
  // darken outer corners toward deep navy
  col *= mix(0.62, 1.0, smoothstep(1.25, 0.35, length(vec2(p.x * 0.85, p.y))));

  // subtle app glow rising from bottom-center (teal/cyan)
  float appGlow = exp(-length(vec2(p.x * 1.1, (p.y + 0.46) * 1.6)) * 2.4);
  col += vec3(0.05, 0.20, 0.26) * appGlow * 0.55;

  // --- bloom above the crest (sky glow) ---
  vec2 bc = p - cp;
  float distC  = length(vec2(bc.x * 1.05, bc.y));
  float distB  = length(vec2(bc.x * 1.7,  bc.y * 0.78));   // vertical dome
  float bloom0 = exp(-distB * 1.95);              // broad soft sky wash
  float bloom1 = exp(-distC * 3.4);
  float bloom2 = exp(-distC * 7.2);
  float bloom3 = exp(-distC * 26.0);
  // core -> cyan #16b9e4 -> teal #5eead4 -> navy, outward
  vec3 skyGlow = vec3(0.0);
  skyGlow += vec3(0.14, 0.52, 0.50) * bloom0 * 0.42;   // outer teal wash
  skyGlow += vec3(0.12, 0.78, 0.86) * bloom1 * 0.60;   // cyan #16b9e4
  skyGlow += vec3(0.52, 0.95, 0.93) * bloom2 * 0.90;   // bright teal-cyan
  skyGlow += vec3(1.00, 1.00, 1.00) * bloom3 * 1.05;   // white-hot core

  // --- very faint god rays fanning up from crest ---
  vec2 rp = p - cp;
  float ang = atan(rp.x, rp.y + 0.001);
  float rays = 0.5 + 0.5 * sin(ang * 26.0 + sin(ang * 7.0) * 1.4 + u_time * 0.04);
  rays = pow(rays, 6.0);
  float rayFall = exp(-distC * 3.0) * smoothstep(-0.02, 0.32, rp.y);
  skyGlow += vec3(0.30, 0.80, 0.92) * rays * rayFall * 0.045;   // cyan god-rays

  col += skyGlow * outside;

  // --- crisp rim of light along the arc ---
  // planet body a touch darker than the sky, so the limb edge reads as a clear horizon
  float planetMask = smoothstep(0.02, -0.05, sd);
  col *= 1.0 - planetMask * 0.25;

  // crisp rim of light along the arc, brightest where the sun crests, fading to the sides
  float edgeFade = smoothstep(1.3, 0.1, abs(p.x));
  float topMask = pow(up, 1.5) * edgeFade;
  float cw = mix(11000.0, 4000.0, up);            // width tapers: thin at ends, wider at crest
  float hw = mix(46.0, 27.0, up);
  float core = exp(-sd * sd * cw);                // crisp bright line, tapering
  float halo = exp(-abs(sd) * hw);                // soft glow, tapering
  col += vec3(0.86, 0.97, 1.00) * core * topMask * 2.5;   // white-hot -> teal-cyan rim
  col += vec3(0.22, 0.74, 0.80) * halo * topMask * 0.58;  // teal-cyan halo

  // --- rising motes ---
  vec3 motes = vec3(0.0);
  float dens = smoothstep(1.05, 0.05, length(vec2(p.x * 0.85, p.y + 0.42)));
  for(int i = 0; i < 2; i++){
    float fi = float(i);
    float sc = 8.0 + fi * 7.0;
    vec2 gp = p * sc;
    gp.y -= u_time * (0.5 + fi * 0.22);
    vec2 id = floor(gp);
    vec2 f = fract(gp);
    vec2 rnd = hash22(id + fi * 27.3);
    float present = step(0.58, hash21(id + fi * 13.7)); // gate ~42% of cells
    vec2 cpt = 0.2 + 0.6 * rnd;
    float dd = length(f - cpt);
    float tw = 0.5 + 0.5 * sin(u_time * (1.5 + 3.0 * rnd.x) + rnd.y * 6.283);
    float m = smoothstep(0.07, 0.0, dd) * tw * present;
    motes += vec3(0.55, 0.92, 0.95) * m * (0.62 - fi * 0.14);   // teal-white sparks
  }
  col += motes * dens * 0.5;

  // --- tonemap + dither ---
  col = vec3(1.0) - exp(-col * 1.25);
  float dn = hash21(gl_FragCoord.xy + fract(u_time)) - 0.5;
  col += dn * (1.6 / 255.0);

  gl_FragColor = vec4(col, 1.0);
}`;

export const BEAM_FRAG = `precision highp float;
uniform vec2 u_res;
uniform float u_time;

// ---- hashes ----
float hash11(float n){ return fract(sin(n)*43758.5453123); }
float hash21(vec2 p){
  p = fract(p*vec2(123.34, 456.21));
  p += dot(p, p+45.32);
  return fract(p.x*p.y);
}

void main(){
  vec2 uv = gl_FragCoord.xy / u_res.xy;
  float asp = u_res.x / u_res.y;
  float x = (uv.x - 0.5) * asp;   // aspect-corrected, centered
  float y = uv.y;                 // 0 bottom .. 1 top
  float t = u_time;

  float dx = x;                   // beam at center
  float flareY = 0.15;            // flare sits near the BOTTOM edge
  // normalized height above the flare: 0 at flare, 1 at the very top
  float h = clamp((y - flareY) / (1.0 - flareY), 0.0, 1.0);

  // vertical envelopes -------------------------------------------------
  // gate that lets the beam sink slightly below the flare then fade to black
  float below = smoothstep(flareY - 0.16, flareY - 0.02, y);
  // glow fades gently up the frame and feathers to near-black at the top
  float glowVert = exp(-h*1.45) * smoothstep(1.06, 0.72, y) * below;
  // white-hot core is strongest at the source, thins toward the top
  float coreVert = exp(-h*2.4) * smoothstep(1.0, 0.86, y) * below;

  // subtle living flicker (visible in the live React canvas)
  float flick = 1.0 + 0.04*sin(t*2.3) + 0.025*sin(t*5.1 + y*6.0);

  // ---- vertical beam: widens gently toward the top -------------------
  float glowW = mix(0.026, 0.145, h);            // teal body sigma
  float glow  = exp(-dx*dx/(2.0*glowW*glowW));
  glow = pow(glow, 1.7);                          // smoother shoulders -> black
  float haloW = mix(0.055, 0.320, h);            // wide soft outer halo
  float halo  = exp(-dx*dx/(2.0*haloW*haloW));
  halo = pow(halo, 1.4);
  // fuller shaft of light near the base that tapers up (beam has body)
  float shaftVert = exp(-h*3.1) * below;
  float shaftW    = mix(0.050, 0.110, h);
  float shaft     = exp(-dx*dx/(2.0*shaftW*shaftW));
  float coreW = mix(0.0016, 0.0042, h);          // white-hot hairline
  float core  = exp(-dx*dx/(2.0*coreW*coreW));
  float sheathW = mix(0.0065, 0.024, h);         // teal-white sheath on core
  float sheath  = exp(-dx*dx/(2.0*sheathW*sheathW));

  // ---- bottom horizontal FLARE (light pouring from below) ------------
  float dyf = y - flareY;
  // wide soft teal wash spanning nearly full width
  float flareFx = exp(-dx*dx/(2.0*0.52*0.52));
  float flareFy = exp(-dyf*dyf/(2.0*0.052*0.052));
  float flare   = flareFx * flareFy;
  // ultra-wide, low secondary bloom bleeding to both edges
  float flareFx2 = exp(-dx*dx/(2.0*0.98*0.98));
  float flareFy2 = exp(-dyf*dyf/(2.0*0.095*0.095));
  float flareWide = flareFx2 * flareFy2;
  // tight white-hot inner flare segment on top of the wash
  float flareCoreFx = exp(-dx*dx/(2.0*0.085*0.085));
  float flareCoreFy = exp(-dyf*dyf/(2.0*0.020*0.020));
  float flareCore   = flareCoreFx * flareCoreFy;

  // ---- sparks / rising motes: spread across the whole beam -----------
  float sparks = 0.0;
  for(int i=0;i<80;i++){
    float fi = float(i);
    float seed  = hash11(fi*13.13);
    float speed = 0.030 + 0.110*hash11(fi*7.77);
    float phase = fract(seed + t*speed);                  // 0 flare .. 1 top
    float my = flareY + phase*(1.0 - flareY);
    // horizontal spread tracks the beam width at this height
    float hy = clamp((my - flareY)/(1.0 - flareY), 0.0, 1.0);
    float spread = 0.010 + hy*0.150;
    float mx = (hash11(fi*3.31)-0.5)*2.0*spread;
    mx += 0.012*sin(t*(0.4+hash11(fi*2.1)) + fi*1.7);     // gentle drift
    vec2 d = vec2(x - mx, y - my);
    d.y *= 0.6;
    float rad = 0.0022 + 0.0034*hash11(fi*9.1);
    float m = smoothstep(rad, 0.0, length(d));
    // per-mote twinkle + fade as they rise + brighter deep in the glow
    float twinkle = 0.35 + 0.65*abs(sin(t*(1.6+2.4*hash11(fi*5.5)) + fi*2.4));
    float fade = (1.0 - phase*0.72) * (0.4 + 0.6*hash11(fi*4.4));
    float inGlow = exp(-mx*mx/(2.0*(spread*1.2)*(spread*1.2)));
    sparks += m * twinkle * fade * inGlow;
  }

  // palette (ChilliCream teal / cyan brand) ---------------------------
  vec3 white = vec3(1.00, 1.00, 1.00);
  vec3 teal  = vec3(0.369, 0.918, 0.831);   // #5eead4 primary glow
  vec3 cyan  = vec3(0.086, 0.725, 0.894);   // #16b9e4 outer halo
  vec3 tealW = vec3(0.62, 0.97, 0.93);      // teal-white for motes/sheath
  vec3 navy  = vec3(0.043, 0.059, 0.102);   // #0b0f1a base it fades to

  vec3 col = vec3(0.0);
  // vertical beam: white-hot core -> teal body -> cyan outer halo -> navy
  col += teal  * shaft  * shaftVert * 0.62 * flick;
  col += teal  * glow   * glowVert * 1.35 * flick;
  col += cyan  * halo   * glowVert * 0.60;
  col += tealW * sheath * coreVert * 1.10 * flick;
  col += white * core   * coreVert * 3.20 * flick;
  // horizontal flare
  col += cyan  * flareWide * 0.48;
  col += teal  * flare     * 1.15;
  col += tealW * flare     * 0.55;
  col += white * flareCore * 2.30 * flick;
  // motes (teal-white)
  col += tealW * sparks * 1.05;

  // tone map + gamma
  col = vec3(1.0) - exp(-col*1.2);
  col = pow(col, vec3(0.90));

  // saturation boost keeps the teal/cyan vivid, not muddy/grey
  float luma = dot(col, vec3(0.299, 0.587, 0.114));
  col = max(vec3(0.0), mix(vec3(luma), col, 1.22));

  // navy floor so corners sit at #0b0f1a, not pure black, and blend into it
  col += navy * (1.0 - smoothstep(0.0, 1.4, length(vec2(dx, (y-0.5)*1.2))));

  // hash dither to kill banding on the shoulders (~1/255)
  float dith = (hash21(gl_FragCoord.xy + fract(t)) - 0.5)/255.0;
  col += dith;

  gl_FragColor = vec4(col, 1.0);
}`;

export const AURORA_FRAG = `precision highp float;
uniform vec2 u_res;
uniform float u_time;

float hash(vec2 p){
  p = fract(p*vec2(123.34, 345.45));
  p += dot(p, p+34.345);
  return fract(p.x*p.y);
}
float vnoise(vec2 p){
  vec2 i = floor(p);
  vec2 f = fract(p);
  vec2 u = f*f*(3.0-2.0*f);
  float a = hash(i);
  float b = hash(i+vec2(1.0,0.0));
  float c = hash(i+vec2(0.0,1.0));
  float d = hash(i+vec2(1.0,1.0));
  return mix(mix(a,b,u.x), mix(c,d,u.x), u.y);
}
float fbm(vec2 p){
  float v=0.0, a=0.5;
  for(int i=0;i<6;i++){ v += a*vnoise(p); p = p*2.02 + 3.1; a *= 0.5; }
  return v;
}

const vec3 C_CYAN   = vec3(0.086,0.725,0.894);  // #16b9e4
const vec3 C_TEAL   = vec3(0.369,0.918,0.831);  // #5eead4
const vec3 C_VIOLET = vec3(0.486,0.573,0.776);  // #7c92c6 brand spectrum
const vec3 C_NAVY   = vec3(0.043,0.059,0.102);  // #0b0f1a base fade

vec3 ramp(float r){
  // white-hot core -> cyan -> teal -> violet spectrum edge -> navy
  vec3 c = mix(vec3(1.0), C_CYAN, smoothstep(0.0, 0.20, r));
  c = mix(c, C_TEAL, smoothstep(0.20, 0.44, r));
  c = mix(c, C_VIOLET, smoothstep(0.44, 0.66, r));
  c = mix(c, C_NAVY, smoothstep(0.66, 0.96, r));
  return c;
}

void main(){
  vec2 fc = gl_FragCoord.xy;
  vec2 res = u_res;
  float x = (fc.x - res.x*0.5)/res.y;   // aspect-correct, centered
  float y = fc.y/res.y;                  // 0 bottom .. 1 top
  float t = u_time;

  float yNeck = 0.28;

  // depth below the neck: 0 at neck, ~1 at the very bottom
  float fy = clamp((yNeck - y)/yNeck, 0.0, 1.0);

  // cone half-width: opens WIDE toward the bottom (fills the frame)
  float halfW = 0.055 + 1.05*pow(fy, 0.92);
  float u = x/halfW;

  // wide across-envelope
  float across = exp(-u*u*1.25);

  // ---- aurora curtains: strong anisotropic fbm (vertical striations) ----
  // low frequency near center, higher toward the edges; noise stretched in y
  float freq = mix(7.0, 16.0, clamp(abs(u), 0.0, 1.0));
  float c1 = fbm(vec2(x*freq + 3.0, y*1.6 - t*0.05));
  float c2 = fbm(vec2(x*freq*1.9 + c1*1.3, y*2.8 - t*0.12));
  float curtain = mix(c1, c2, 0.42);
  float streak = smoothstep(0.26, 0.75, curtain);      // sharpen into curtains
  float fine = vnoise(vec2(x*58.0, y*1.8 - t*0.10));    // fine vertical striations
  float bands = (0.28 + 1.18*streak) * (0.80 + 0.34*fine);

  // vertical profile: brightest near the neck, curtains persist toward the base
  float vprof = 0.42 + 0.58*exp(-fy*1.15);
  float baseFade = smoothstep(0.0, 0.16, y);   // no hard line at y=0
  float fanEnv = fy * vprof * baseFade;

  float fan = across * bands * fanEnv;

  // ---- beam: thin bright line from top down to the neck ----
  float above = clamp((y - yNeck)/(1.0-yNeck), 0.0, 1.0);
  float beamW = mix(0.010, 0.0035, above);
  float beam = beamW*beamW/(beamW*beamW + x*x);
  beam *= (0.6 + 0.5*fbm(vec2(x*24.0, y*3.0 - t*0.4)));
  beam *= smoothstep(-0.02, 0.30, y);
  beam *= (0.52 + 0.48*smoothstep(1.05, 0.26, y));

  // ---- white-hot elongated core sliver at the neck ----
  float dy = y - yNeck;
  float coreSliver = exp(-(x*x)/0.00052 - (dy*dy)/(dy>0.0?0.010:0.020));

  // ---- radial metrics around the core ----
  float radc = length(vec2(x, dy*1.3));

  // ---- god rays fanning from the white-hot core ----
  float ang = atan(x, -(dy)*1.5 + 0.0001);
  float rays = fbm(vec2(ang*7.0, radc*3.0 - t*0.12));
  rays = pow(clamp(rays,0.0,1.0), 1.8);
  float rayGlow = rays * exp(-radc*4.2) * smoothstep(0.015, 0.18, radc);

  // ---- soft additive bloom halo around the core ----
  float halo = 0.50*exp(-radc*5.2) + 0.34*exp(-radc*10.5) + 0.18*exp(-radc*2.4) + 0.06*exp(-radc*1.2);

  // ---- intensity field ----
  float E = fan*1.30 + beam*1.20 + coreSliver*2.0 + halo*0.80 + rayGlow*0.55;

  // ---- hue by radius from core: white -> cyan -> teal -> violet -> navy ----
  float hueR = abs(u)*0.66 + radc*0.34 + fy*0.14;
  vec3 hue = ramp(hueR);

  vec3 col = hue * E;

  // white-hot core & soft bloom (additive)
  col += vec3(1.0) * smoothstep(1.25, 2.15, E);
  col += vec3(0.80,0.95,1.0) * halo * 0.28 * clamp(across+0.15,0.0,1.0);

  // extra teal saturation in the mid-radius band, violet whisper toward the edge
  float tband = smoothstep(0.20,0.44,hueR) * smoothstep(0.66,0.40,hueR);
  col += C_TEAL * E * tband * 0.42;
  float vband = smoothstep(0.48,0.68,hueR) * smoothstep(0.94,0.66,hueR);
  col += C_VIOLET * E * vband * 0.30;

  // ---- rising sparks (additive white-blue points, denser near core) ----
  float sp = 0.0;
  for(int i=0;i<3;i++){
    float fi = float(i);
    float cell = 24.0 + fi*13.0;
    vec2 gp = vec2(x*cell + fi*17.0, y*cell*1.5 - t*(7.0+fi*3.0));
    vec2 ip = floor(gp); vec2 fp = fract(gp);
    float h = hash(ip + fi*37.0);
    float life = fract(h*7.31 + t*(0.35+0.12*fi));
    vec2 pc = fp - vec2(0.5, 0.5);
    float d = dot(pc, pc);
    float size = mix(0.055, 0.012, life);        // shrink over lifetime
    float pt = smoothstep(size, 0.0, d) * (1.0-life) * step(0.90, h);
    sp += pt;
  }
  sp *= (0.35 + 0.65*across) * fanEnv * 2.0;
  col += vec3(0.70,0.96,0.94) * sp;

  // faint cosmic haze in the dark field, tinted toward navy/teal
  float neb = fbm(vec2(x*1.6 + 9.0, y*1.8 - t*0.02));
  neb = smoothstep(0.55, 1.0, neb) * smoothstep(0.1, 0.7, y) * 0.06;
  col += mix(C_NAVY, C_TEAL, 0.35) * neb;

  // lift the darkest field to brand navy so the effect fades to #0b0f1a, not black
  col += C_NAVY;

  // exposure tone map: keeps blacks black, rolls off highlights
  col = vec3(1.0) - exp(-col*1.35);

  // dither to kill banding
  col += (hash(fc + t) - 0.5)/210.0;

  col = clamp(col, 0.0, 1.0);
  gl_FragColor = vec4(col, 1.0);
}`;
