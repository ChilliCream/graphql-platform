exports.onCreateWebpackConfig = (
  { actions, getConfig, rules },
  options = {}
) => {
  const config = getConfig();
  const imagesRule = rules.images();
  const imagesRuleTest = String(imagesRule.test);

  const { rule, ...rest } = options;

  config.module.rules = [
    ...config.module.rules.filter(
      (item) => String(item.test) !== imagesRuleTest
    ),
    {
      ...rule,
      use: [
        {
          loader: require.resolve("svg-sprite-loader"),
          options: rest,
        },
      ],
    },
    {
      ...imagesRule,
      test: new RegExp(imagesRuleTest.replace("svg|", "").slice(1, -1)),
    },
  ];

  actions.replaceWebpackConfig(config);
};
