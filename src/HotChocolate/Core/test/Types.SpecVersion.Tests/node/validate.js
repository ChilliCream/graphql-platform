const fs = require("node:fs");

const [alias, schemaPath] = process.argv.slice(2);

if (!alias || !schemaPath) {
  console.error("Usage: node validate.js <alias> <sdl-path>");
  process.exit(1);
}

try {
  const { assertValidSchema, buildSchema } = require(alias);
  const schema = buildSchema(fs.readFileSync(schemaPath, "utf8"));
  assertValidSchema(schema);
} catch (error) {
  console.error(error.message);
  process.exit(1);
}
