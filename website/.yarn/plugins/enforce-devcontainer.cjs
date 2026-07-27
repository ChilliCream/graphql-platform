module.exports = {
  name: `plugin-enforce-devcontainer`,
  factory: () => ({
    hooks: {
      validateProject(project, report) {
        if (!process.env.CHILLICREAM_FRONTEND_ENV) {
          report.reportError(
            0,
            `yarn install is only allowed inside the frontend devcontainer or CI. ` +
              `Open the "ChilliCream Frontend" devcontainer for development on the website.`,
          );
        }
      },
    },
  }),
};
