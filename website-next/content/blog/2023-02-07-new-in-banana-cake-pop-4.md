---
path: "/blog/2023/02/07/new-in-banana-cake-pop-4"
date: "2023-02-07"
title: "New in Banana Cake Pop 4"
description: "New document on paste cURL or fetch, schema reload enhancements, new ways of closing tabs, menu enhancements/standardization, and UI polishing."
tags: ["bananacakepop", "graphql", "ide", "cloud", "release"]
featuredImage: "new-in-banana-cake-pop-4.png"
author: Rafael Staib
authorUrl: https://github.com/rstaib
authorImageUrl: https://avatars0.githubusercontent.com/u/4325318?s=100&v=4
---

In version 4, we mainly focused on polishing and fixing UI glitches to improve the overall user experience, but that doesn't mean there isn't any new feature. To download **Banana Cake Pop 4**, go to [bananacakepop.com](https://bananacakepop.com). Let me walk you through the most important things we did in Version 4.

## Paste cURL and Fetch

Almost any IDE or Tool nowadays allows copying HTTP requests as cURL or fetch. So why not make use of it? Yeah, that's what we thought too. With version 4, we support pasting `cURL` and `fetch` GraphQL requests into **Banana Cake Pop**.

When pasting such a GraphQL request, a new document will be created with all its HTTP headers, GraphQL variables, GraphQL operation, and the GraphQL endpoint.

This is very helpful in various scenarios, especially when working with the Chrome Developer Tools to identify GraphQL request issues. Just go to the Network tab, right-click on any HTTP request, and then _copy as cURL_. As long as the copied HTTP request is a GraphQL request, **Banana Cake Pop** will create a new document on paste.

There are two ways of creating a new document for a copied `cURL` or `fetch` GraphQL request. First, use the shortcut. `CMD + OPT + V` on macOS and `CTRL + ALT + V` on windows. Second, click the three dots icon next to the save button for tabs, and then click _New document from clipboard_. Ta-da, that's it!

## Schema Reload

Sometimes, reloading a schema takes just a couple of milliseconds, which makes it rather impossible to see the loading indicator spinning. In general, feedback is critical when clicking a button, so we know whether we clicked it. The same goes for the schema reload button. Without knowing whether we clicked it, we’ll click it again and again. In the end, we come to the conclusion that the schema reload does not work. In fact, this isn't true, but how should we know?

Of course, we solved this issue by adding a decent pulse effect to the schema reload button, which keeps going for a couple of seconds.

Additionally, we merged the schema reload button with the schema fetch status to keep things clear and compact.

Furthermore, we improved the schema fetch status, including its tooltip in the status bar, which makes things more explicit.

## Close Multiple Document Tabs

Finally, after quite some time, we've added a bunch of ways to close multiple document tabs simultaneously. Right-click on a document tab and choose between _close others_, _close to the right_, and _close all_. Enjoy!

## Menu Enhancements

We've standardized and enhanced the menu component. We've fixed positioning issues and glitches. Also, we introduce navigation by key (arrow up and down).

## Schema Reference Default Values

Default values for fields in the schema reference column and type view are now available.

## Become An Insider

Hey you, we're looking for you to become an _Insider_ to help us shape the future of **Banana Cake Pop**. We're constantly pushing new _Insider_ builds, sometimes even daily. Get early access to new features, or help us find that last-minute show-stopper issue. Go to [bananacakepop.com](https://bananacakepop.com) to get the latest version of the _Insider_ app or check out the online web version on [insider.bananacakepop.com](https://insider.bananacakepop.com) instead.

## Subscribe

To stay up to date, subscribe to our [ChilliCream YouTube Channel](https://www.youtube.com/c/ChilliCream) to get notified whenever we publish new videos.

I'm Rafael Staib, and as soon as **Banana Cake Pop 5** is released, I'll be right here to tell you what's new in **Banana Cake Pop**!
