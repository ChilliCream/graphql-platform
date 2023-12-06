## Guidelines for writing documentation

- When introducing links to other documentation pages, include the version, e.g. `/docs/hotchocolate/v12/some-page`.

## Creating a new documentation page

1. Locate the directory of the product version you want to add a documentation entry for, e.g. `src/docs/hotchocolate/v12`.
2. Create a new Markdown file in one of the appropriate categories (or create a new one).
3. Open the `src/docs/docs.json` file and locate the array item where the value of the `path` property matches the product.
4. Inside of the `version` array, find the item where the value of the `path` property matches the version.
5. Locate (or create) the correct category object in the `items` property and add a new object to its `items` property:

```json
{
  "path": "your_markdown_filename_without_the_extensions",
  "title": "The title of your document"
},
```

6. Finish the Markdown file.

## Creating a new documentation version

1. Create a new directory for the new version inside of the product directory, e.g. `src/docs/hotchocolate/v13`.
2. Copy the contents of the previous version directory into the newly created version.
3. Open just the new version directory in a separate VS Code instance.
4. Search for `/docs/hotchocolate/v12/` and replace it with `/docs/hotchocolate/v13/`. (`hotchocolate` being the product, `v12` the previous version and `v13` the new version)

> Note: Links in the `Migrating` section shouldn't be updated.

5. Open the `src/docs/docs.json` file and locate the array item where the value of the `path` property matches the product.
6. Inside of the `version` array, copy and paste the object where the value of the `path` property matches the previous version.
7. In the duplicated object, update both the `path` and `title` property to the new version.
8. Optionally adjust the [default documentation version of the product](#changing-the-default-documentation-version).

## Changing the default documentation version

1. Open the `src/docs/docs.json` file and locate the array item where the value of the `path` property matches the product.
2. Adjust the value of the `latestStableVersion` property.
3. Open the `config/conf.d/default.conf` file and adjust the value of the `$latest` variable.
