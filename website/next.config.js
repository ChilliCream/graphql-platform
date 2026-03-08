const path = require("path");

/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "export",
  trailingSlash: true,
  images: {
    unoptimized: true,
  },
  compiler: {
    styledComponents: true,
  },
  typescript: {
    ignoreBuildErrors: true,
  },
  transpilePackages: [
    "@chillicream/mocha-visualizer",
    "@docsearch/react",
    "next-mdx-remote",
    "@mdx-js/react",
  ],
  webpack(config) {
    // Ensure peer dependencies of the portal-linked mocha-visualizer resolve
    // from the website's node_modules (portal packages live outside this directory).
    const websiteModules = path.resolve(__dirname, "node_modules");
    config.resolve.alias = {
      ...config.resolve.alias,
      "@xyflow/react": path.resolve(websiteModules, "@xyflow/react"),
      "@fortawesome/react-fontawesome": path.resolve(
        websiteModules,
        "@fortawesome/react-fontawesome"
      ),
      "@fortawesome/fontawesome-svg-core": path.resolve(
        websiteModules,
        "@fortawesome/fontawesome-svg-core"
      ),
      "@fortawesome/free-solid-svg-icons": path.resolve(
        websiteModules,
        "@fortawesome/free-solid-svg-icons"
      ),
      elkjs: path.resolve(websiteModules, "elkjs"),
      "styled-components": path.resolve(websiteModules, "styled-components"),
    };

    // SVG sprite loader for artwork, companies, icons, logo directories
    const spriteRule = {
      test: /images\/(artwork|companies|icons|logo)\/.*\.svg$/,
      use: [
        {
          loader: "svg-sprite-loader",
        },
      ],
    };

    // SVGR for all other SVGs
    const svgrRule = {
      test: /\.svg$/,
      exclude: /images\/(artwork|companies|icons|logo)\/.*\.svg$/,
      use: ["@svgr/webpack"],
    };

    // Remove Next.js default SVG handling
    const fileLoaderRule = config.module.rules.find(
      (rule) => rule.test instanceof RegExp && rule.test.test(".svg")
    );
    if (fileLoaderRule) {
      fileLoaderRule.exclude = /\.svg$/;
    }

    config.module.rules.push(spriteRule, svgrRule);

    return config;
  },
};

module.exports = nextConfig;
