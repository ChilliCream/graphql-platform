const path = require("path");

module.exports = {
  target: "node",
  entry: "./src/index.ts",
  mode: "production",
  module: {
    rules: [
      {
        test: /\.ts$/,
        use: "ts-loader",
        exclude: /node_modules/,
      },
    ],
  },
  resolve: {
    extensions: [".ts", ".js"],
  },
  output: {
    filename: "executable.js",
    path: path.resolve(__dirname, "build"),
  },
};
