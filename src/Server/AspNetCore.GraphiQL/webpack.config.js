const path = require("path");

const HtmlWebpackPlugin = require("html-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CopyWebpackPlugin = require("copy-webpack-plugin");

module.exports = {
  mode: "production",
  context: path.join(__dirname, "./app"),
  entry: {
    index: "./index.js",
  },
  output: {
    path: path.join(__dirname, "Resources"),
    filename: "[name].js",
    sourceMapFilename: "[name].js.map",
  },
  resolve: {
    extensions: [".mjs", ".js", ".css"],
    modules: [path.join(__dirname, "src"), "node_modules"],
  },
  module: {
    rules: [
      {
        test: /\.js$/,
        exclude: /node_modules/,
        use: [
          {
            loader: "babel-loader",
            options: {
              presets: ["@babel/preset-env", "@babel/preset-react"],
            },
          },
        ],
      },
      {
        test: /\.css$/,
        use: [MiniCssExtractPlugin.loader, "css-loader"],
      },
    ],
  },
  plugins: [
    new HtmlWebpackPlugin({
      template: path.join(__dirname, "app", "index.html"),
      filename: "index.html",
      chunks: ["index"],
    }),
    new MiniCssExtractPlugin({
      filename: "style.css",
    }),
    new CopyWebpackPlugin([
      {
        from: "./favicon.ico",
        to: path.resolve(__dirname, "Resources"),
        toType: "dir",
      },
    ]),
  ],
  devtool: "source-map",
};
