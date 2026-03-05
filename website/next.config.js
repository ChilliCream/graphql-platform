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
  transpilePackages: ["@docsearch/react", "next-mdx-remote", "@mdx-js/react"],
  webpack(config) {
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
