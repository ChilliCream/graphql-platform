import { FC, ReactElement, ReactNode, useLayoutEffect, useRef } from "react";

import { CookieConsent, Promo } from "@/components/misc";
import { GlobalStyle } from "@/style";
import { Main } from "./main";

export interface SiteLayoutProps {
  readonly children: ReactNode;
  readonly disableStars?: boolean;
}

export const SiteLayout: FC<SiteLayoutProps> = ({ children, disableStars }) => {
  return (
    <>
      <GlobalStyle />
      <Main>{children}</Main>
      <CookieConsent />
      <Promo />
      {!disableStars && <Stars />}
    </>
  );
};

function Stars(): ReactElement {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const blackHoleRef = useRef<SVGSVGElement>(null);

  useLayoutEffect(() => {
    if (!canvasRef.current) {
      return;
    }

    const glCanvas = canvasRef.current;
    const gl = glCanvas.getContext("webgl", {
      alpha: true,
      premultipliedAlpha: false,
    });

    if (!gl) {
      return;
    }

    const starCanvas = document.createElement("canvas");
    const ctx = starCanvas.getContext("2d");

    if (!ctx) {
      return;
    }

    const numStars = 800;
    const speed = 0.25;
    let stars: Star[] = [];
    let mouseX = 0;
    let mouseY = 0;
    let forceTarget = 0;
    let forceCurrent = 0;

    function setCanvasSize() {
      const w = window.innerWidth;
      const h = window.innerHeight;

      glCanvas.width = w;
      glCanvas.height = h;
      starCanvas.width = w;
      starCanvas.height = h;
      gl!.viewport(0, 0, w, h);
    }

    class Star {
      constructor(
        private x: number,
        private y: number,
        private z: number,
        private size: number
      ) {}

      update() {
        this.z -= speed;
        if (this.z <= 0) {
          this.reset();
        }
      }

      reset() {
        this.z = starCanvas.width;
        this.x = Math.random() * (starCanvas.width * 2) - starCanvas.width;
        this.y = Math.random() * (starCanvas.height * 2) - starCanvas.height;
        this.size = Math.random() * 2 + 1;
      }

      draw() {
        const x =
          ((this.x / this.z) * starCanvas.width) / 2 + starCanvas.width / 2;
        const y =
          ((this.y / this.z) * starCanvas.height) / 2 + starCanvas.height / 2;
        const radius = (1 - this.z / starCanvas.width) * this.size;

        ctx!.beginPath();
        ctx!.arc(x, y, radius, 0, Math.PI * 2);
        ctx!.fill();
      }
    }

    function initStars() {
      stars = Array.from(
        { length: numStars },
        () =>
          new Star(
            Math.random() * (starCanvas.width * 2) - starCanvas.width,
            Math.random() * (starCanvas.height * 2) - starCanvas.height,
            Math.random() * starCanvas.width,
            Math.random() * 2 + 1
          )
      );
    }

    const hasHover = window.matchMedia("(hover: hover)").matches;

    const vertSrc = `
      attribute vec2 aPos;
      varying vec2 vUv;
      void main() {
        vUv = (aPos + 1.0) * 0.5;
        gl_Position = vec4(aPos, 0.0, 1.0);
      }
    `;

    const noiseHelpers = `
      float hash(vec2 p) {
        return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
      }

      float noise(vec2 p) {
        vec2 i = floor(p);
        vec2 f = fract(p);
        vec2 u = f * f * (3.0 - 2.0 * f);
        return mix(
          mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x),
          mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x),
          u.y
        );
      }

      float fbm(vec2 p) {
        float v = 0.0;
        float a = 0.5;
        for (int i = 0; i < 5; i++) {
          v += a * noise(p);
          p *= 2.0;
          a *= 0.5;
        }
        return v;
      }

      float fbm3(vec2 p) {
        float v = 0.0;
        float a = 0.5;
        for (int i = 0; i < 3; i++) {
          v += a * noise(p);
          p *= 2.0;
          a *= 0.5;
        }
        return v;
      }
    `;

    const fragDesktopSrc = `
      precision mediump float;
      uniform vec2 u_mouse;
      uniform vec2 u_resolution;
      uniform sampler2D u_texture;
      uniform float u_force;
      uniform float u_time;
      varying vec2 vUv;

      ${noiseHelpers}

      void main() {
        vec2 st = vUv;
        float aspect = u_resolution.x / u_resolution.y;

        vec2 distortedUv;
        if (u_force < 0.001) {
          distortedUv = st;
        } else {
          vec2 mouse = u_mouse / u_resolution;
          mouse.y = 1.0 - mouse.y;
          vec2 scaledSt = st * vec2(aspect, 1.0);
          float dist = distance(scaledSt, mouse * vec2(aspect, 1.0));
          float distortion = u_force / max(dist, 0.0001);
          distortedUv = st + (mouse - st) * distortion;
        }

        vec4 stars = texture2D(u_texture, distortedUv);

        vec2 nebUv = distortedUv * vec2(aspect, 1.0);
        float t = u_time;
        float n1 = fbm(nebUv * 2.2 + vec2(t * 0.012, t * 0.008));
        float n2 = fbm3(nebUv * 5.5 + vec2(31.7 - t * 0.018, 11.3 + t * 0.010));
        float n3 = fbm3(nebUv * 3.8 + vec2(-7.9 + t * 0.014, 22.5 - t * 0.009));
        float n4 = fbm3(nebUv * 4.3 + vec2(54.1 + t * 0.007, -18.7 + t * 0.016));
        float n5 = fbm3(nebUv * 6.1 + vec2(-23.9 - t * 0.013, 47.2 - t * 0.006));
        float n6 = fbm3(nebUv * 2.8 + vec2(67.4 + t * 0.005, 3.6 + t * 0.011));
        float n7 = fbm3(nebUv * 4.8 + vec2(-41.2 + t * 0.009, -62.5 - t * 0.013));
        float n8 = fbm3(nebUv * 5.0 + vec2(88.6 - t * 0.011, 29.4 + t * 0.015));

        vec3 deep = vec3(0.10, 0.07, 0.22);
        vec3 violet = vec3(0.35, 0.22, 0.60);
        vec3 blueGrey = vec3(0.30, 0.45, 0.80);
        vec3 coral = vec3(0.95, 0.65, 0.55);
        vec3 magenta = vec3(0.85, 0.32, 0.65);
        vec3 teal = vec3(0.15, 0.68, 0.78);
        vec3 amber = vec3(0.95, 0.75, 0.35);
        vec3 red = vec3(0.92, 0.26, 0.30);
        vec3 yellow = vec3(0.98, 0.88, 0.35);

        vec3 nebulaColor = deep;
        nebulaColor = mix(nebulaColor, violet, smoothstep(0.30, 0.80, n1));
        nebulaColor = mix(nebulaColor, blueGrey, smoothstep(0.55, 0.90, n2) * 0.65);
        nebulaColor = mix(nebulaColor, teal, smoothstep(0.62, 0.92, n5) * 0.55);
        nebulaColor = mix(nebulaColor, magenta, smoothstep(0.68, 0.92, n4) * 0.45);
        nebulaColor = mix(nebulaColor, coral, smoothstep(0.72, 0.95, n3) * 0.40);
        nebulaColor = mix(nebulaColor, red, smoothstep(0.78, 0.95, n7) * 0.40);
        nebulaColor = mix(nebulaColor, amber, smoothstep(0.78, 0.96, n6) * 0.30);
        nebulaColor = mix(nebulaColor, yellow, smoothstep(0.82, 0.97, n8) * 0.35);

        float nebulaAlpha = 0.20 + n1 * 0.38;

        float starsA = smoothstep(0.02, 0.5, stars.a);
        vec3 preRgb =
          stars.rgb * starsA +
          nebulaColor * nebulaAlpha * (1.0 - starsA);
        float outAlpha = starsA + nebulaAlpha * (1.0 - starsA);
        gl_FragColor = vec4(preRgb, outAlpha);
      }
    `;

    const fragMobileSrc = `
      precision mediump float;
      uniform vec2 u_resolution;
      uniform sampler2D u_texture;
      uniform float u_time;
      varying vec2 vUv;

      ${noiseHelpers}

      void main() {
        vec2 st = vUv;
        float aspect = u_resolution.x / u_resolution.y;

        vec4 stars = texture2D(u_texture, st);

        vec2 nebUv = st * vec2(aspect, 1.0);
        float t = u_time;
        float n1 = fbm3(nebUv * 2.2 + vec2(t * 0.012, t * 0.008));
        float n2 = fbm3(nebUv * 5.5 + vec2(31.7 - t * 0.018, 11.3 + t * 0.010));
        float n3 = fbm3(nebUv * 3.8 + vec2(-7.9 + t * 0.014, 22.5 - t * 0.009));
        float n4 = fbm3(nebUv * 4.3 + vec2(54.1 + t * 0.007, -18.7 + t * 0.016));
        float n5 = fbm3(nebUv * 6.1 + vec2(-23.9 - t * 0.013, 47.2 - t * 0.006));

        vec3 deep = vec3(0.10, 0.07, 0.22);
        vec3 violet = vec3(0.35, 0.22, 0.60);
        vec3 blueGrey = vec3(0.30, 0.45, 0.80);
        vec3 coral = vec3(0.95, 0.65, 0.55);
        vec3 magenta = vec3(0.85, 0.32, 0.65);
        vec3 teal = vec3(0.15, 0.68, 0.78);

        vec3 nebulaColor = deep;
        nebulaColor = mix(nebulaColor, violet, smoothstep(0.30, 0.80, n1));
        nebulaColor = mix(nebulaColor, blueGrey, smoothstep(0.55, 0.90, n2) * 0.65);
        nebulaColor = mix(nebulaColor, teal, smoothstep(0.62, 0.92, n5) * 0.55);
        nebulaColor = mix(nebulaColor, magenta, smoothstep(0.68, 0.92, n4) * 0.45);
        nebulaColor = mix(nebulaColor, coral, smoothstep(0.72, 0.95, n3) * 0.40);

        float nebulaAlpha = 0.20 + n1 * 0.38;

        float starsA = smoothstep(0.02, 0.5, stars.a);
        vec3 preRgb =
          stars.rgb * starsA +
          nebulaColor * nebulaAlpha * (1.0 - starsA);
        float outAlpha = starsA + nebulaAlpha * (1.0 - starsA);
        gl_FragColor = vec4(preRgb, outAlpha);
      }
    `;

    const fragSrc = hasHover ? fragDesktopSrc : fragMobileSrc;

    function compile(type: number, src: string): WebGLShader {
      const sh = gl!.createShader(type)!;
      gl!.shaderSource(sh, src);
      gl!.compileShader(sh);
      return sh;
    }

    const vs = compile(gl.VERTEX_SHADER, vertSrc);
    const fs = compile(gl.FRAGMENT_SHADER, fragSrc);
    const program = gl.createProgram()!;

    gl.attachShader(program, vs);
    gl.attachShader(program, fs);
    gl.linkProgram(program);
    gl.useProgram(program);

    const vbo = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, vbo);

    gl.bufferData(
      gl.ARRAY_BUFFER,
      new Float32Array([-1, -1, 1, -1, -1, 1, -1, 1, 1, -1, 1, 1]),
      gl.STATIC_DRAW
    );

    const aPosLoc = gl.getAttribLocation(program, "aPos");

    gl.enableVertexAttribArray(aPosLoc);
    gl.vertexAttribPointer(aPosLoc, 2, gl.FLOAT, false, 0, 0);

    const uMouseLoc = gl.getUniformLocation(program, "u_mouse");
    const uResLoc = gl.getUniformLocation(program, "u_resolution");
    const uForceLoc = gl.getUniformLocation(program, "u_force");
    const uTexLoc = gl.getUniformLocation(program, "u_texture");
    const uTimeLoc = gl.getUniformLocation(program, "u_time");
    const startTime = performance.now();
    const tex = gl.createTexture();

    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
    gl.uniform1i(uTexLoc, 0);

    gl.enable(gl.BLEND);
    gl.blendFunc(gl.ONE, gl.ONE_MINUS_SRC_ALPHA);
    gl.clearColor(0, 0, 0, 0);

    function animate() {
      ctx!.clearRect(0, 0, starCanvas.width, starCanvas.height);
      ctx!.fillStyle = "#f4ebcb";
      stars.forEach((s) => s.update());
      stars.forEach((s) => s.draw());

      gl!.bindTexture(gl!.TEXTURE_2D, tex);
      gl!.texImage2D(
        gl!.TEXTURE_2D,
        0,
        gl!.RGBA,
        gl!.RGBA,
        gl!.UNSIGNED_BYTE,
        starCanvas
      );

      gl!.clear(gl!.COLOR_BUFFER_BIT);
      gl!.uniform2f(uResLoc, glCanvas.width, glCanvas.height);
      gl!.uniform1f(uTimeLoc, (performance.now() - startTime) / 1000);
      if (hasHover) {
        forceCurrent += (forceTarget - forceCurrent) * 0.1;
        gl!.uniform2f(uMouseLoc, mouseX, mouseY);
        gl!.uniform1f(uForceLoc, forceCurrent);
      }
      gl!.drawArrays(gl!.TRIANGLES, 0, 6);

      requestAnimationFrame(animate);
    }

    function handlePointerMove(e: PointerEvent) {
      mouseX = e.clientX;
      mouseY = e.clientY;
      forceTarget = 0.08;

      if (blackHoleRef.current) {
        blackHoleRef.current.style.transform = `translate3d(${e.clientX}px, ${e.clientY}px, 0) translate(-50%, -50%)`;
        blackHoleRef.current.style.opacity = "1";
      }
    }

    function handlePointerLeave() {
      forceTarget = 0;

      if (blackHoleRef.current) {
        blackHoleRef.current.style.opacity = "0";
      }
    }

    window.addEventListener("resize", () => {
      setCanvasSize();
      initStars();
    });

    if (hasHover) {
      window.addEventListener("pointermove", handlePointerMove, {
        passive: true,
      });
      document.addEventListener("pointerleave", handlePointerLeave);
    }

    setCanvasSize();
    initStars();
    animate();
  }, []);

  return (
    <>
      <canvas
        ref={canvasRef}
        style={{
          position: "absolute",
          top: 0,
          right: 0,
          bottom: 0,
          left: 0,
          zIndex: -2,
          width: "100vw",
          height: "100vh",
        }}
      />
      <svg
        ref={blackHoleRef}
        width="60"
        height="60"
        viewBox="-30 -30 60 60"
        style={{
          position: "fixed",
          top: 0,
          left: 0,
          zIndex: -2,
          pointerEvents: "none",
          transform: "translate3d(-9999px, -9999px, 0)",
          opacity: 0,
          transition: "opacity 300ms ease",
        }}
      >
        <circle cx="0" cy="0" r="25" fill="#000" />
      </svg>
    </>
  );
}
