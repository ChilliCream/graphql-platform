exports.onCreateWebpackConfig = (
  { actions, loaders, getConfig },
  options = {}
) => {
  const prevConfig = getConfig();

  const { rule, ...rest } = options;

  actions.replaceWebpackConfig({
    ...prevConfig,
    module: {
      ...prevConfig.module,
      rules: [
        ...prevConfig.module.rules.map((item) => {
          const { test } = item;

          if (
            test &&
            test.toString() === "/\\.(ico|svg|jpg|jpeg|png|gif|webp)(\\?.*)?$/"
          ) {
            return {
              ...item,
              test: /\.(ico|jpg|jpeg|png|gif|webp)(\?.*)?$/,
            };
          }

          return { ...item };
        }),
        {
          test: /\.svg$/,
          ...rule,
          use: [
            {
              loader: require.resolve("svg-sprite-loader"),
              options: rest,
            },
          ],
        },
      ],
    },
  });
};
