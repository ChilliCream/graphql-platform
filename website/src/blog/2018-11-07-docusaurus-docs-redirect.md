---
path: "/blog/2018/11/07/docusaurus-docs-redirect"
date: "2018-11-07"
title: "Docusaurus - How to redirect requests to /docs to a default url instead of getting a 404 error"
author: "Rafael Staib"
authorUrl: https://github.com/rstaib
authorImageUrl: https://avatars0.githubusercontent.com/u/4325318?s=100&v=4
---

I recently run into an HTTP 404 error when calling /docs on my _Docusaurus_ websites. This isn't
actually nice, because I expected to land on my documentation entry page e.g. introduction. First I
thought, perhaps this is an issue with my setup. But I found out that even the _Docusaurus_ website
itself is suffering from this issue as well. So I tried to find a solution on the internet. But
I couldn't find anything except an issue on github describing the same behavior. So, with this article
I try to help everyone saving their time and making the experience with _Docusaurus_ even better.

So here is my solution.

1. Go to your `website\siteConfig.js` file and update the entry doc link in the `headerLinks`
   section by adding `href: "/docs"` to it.

**Before**

```javascript
{
  // code omitted for brevity
  headerLinks: [
    {
      doc: "your-entry-doc",
      label: "Docs",
    },
  ];
}
```

**After**

```javascript
{
  // code omitted for brevity
  headerLinks: [
    {
      doc: "your-entry-doc",
      href: "/docs",
      label: "Docs",
    },
  ];
}
```

2. Create a new file called `docs.js` under the `website\pages\en` path and insert the following
   code.

```javascript
/**
 * Copyright (c) 2017-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

const React = require("react");
const Redirect = require("../../core/Redirect.js");

const siteConfig = require(process.cwd() + "/siteConfig.js");

function docUrl(doc, language) {
  return (
    siteConfig.baseUrl +
    "docs/" +
    (language ? language + "/" : "") +
    doc +
    ".html"
  );
}

class Docs extends React.Component {
  render() {
    return (
      <Redirect
        redirect={docUrl("your-entry-doc", this.props.language)}
        config={siteConfig}
      />
    );
  }
}

module.exports = Docs;
```

This code is just doing a redirect to `/docs/your-entry-doc`. Don't forget to replace
`your-entry-doc` with your own value.

Perfect! With this little change, our _Docusaurus_ website is now able to handle requests to the
`/docs` root path.

One little thing: I have tested it with _Docusaurus_ version `1.5.1`. However, just try it!
