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

In version 4 we focused mostly on polishing and fixing UI glitches to improve the overall user experience, but that doesn't mean, that there isn't any new feature. To download **Banana Cake Pop 4** go to [bananacakepop.com](https://bananacakepop.com). Now let me walk you through to the most important things we did in Version 4.

## Paste cURL and Fetch

Almost any IDE or Tool nowadays allows copying HTTP requests as cURL or fetch. So why not making use of it? Yeah, that's what we thought too. With version 4 we support pasting cURL and fetch GraphQL requests into **Banana Cake Pop**.

When pasting such GraphQL request, a new document will be created with all its HTTP headers, GraphQL variables, GraphQL operation, and of course the GraphQL endpoint.

This is very helpful in various scenarios, but especially when working with the Chrome Developer Tools to identify GraphQL request issues. Just go to the Network tab, right click on any HTTP request, and then _copy as cURL_. As long as the copied HTTP request is a GraphQL request, **Banana Cake Pop** will create a new document out of it on paste.

There are two ways of creating a new document for a copied cURL or fetch GraphQL request. First, use the shortcut. `CMD + OPT + V` on macOS and `CTRL + ALT + V` on windows. Second, click the three dots icon right next to the save button for tabs, and then click _New document from clipboard_. Ta da that's it!

## Schema Reload

Sometimes, reloading a schema takes just a few couple milliseconds, which makes it rather impossible to see the loading indicator spinning. In general feedback is very important when clicking a button, so that we know whether we clicked it. Same goes for the schema reload button. Without knowing whether we clicked it, we click again, and again. In the end we come to the conclusion that the schema reload does not work. In fact this isn't true, but how should we know?

We solved this issue of course by adding a decent pulse effect to the schema reload button which keeps on going for a couple of seconds.

Additionally, we merged the schema reload button with the schema fetch status to keep things clear and compact.

Furthermore, we improved the schema fetch status including its tooltip in the status bar which makes things more explicit.

## Close Multiple Document Tabs

Finally, after quite some time, we've added a bunch of ways to close multiple document tabs at once. For that right click on a document tab an choose between _close others_, _close to the right_, _close all_. Enjoy!

## Menu Enhancements

## Schema Reference Default Values

## Become An Insider

Hey you, we're looking for you becoming an _Insider_ to help us shaping the future of **Banana Cake Pop**. We're constantly pushing new _Insider_ builds sometimes even on a daily base. Get early access to new features, or help us find that last-minute show stopper issue. Go to [bananacakepop.com](https://bananacakepop.com) to get the latest version of the _Insider_ app or check out the online web version on [insider.bananacakepop.com](https://insider.bananacakepop.com) instead.

## Subscribe

To stay up to date, subscribe to our [ChilliCream YouTube Channel](https://www.youtube.com/c/ChilliCream) to get notified whenever we publish new videos.

I'm Rafael Staib, and as soon as **Banana Cake Pop 5** is released, I'll be right here to tell you what's new in **Banana Cake Pop**!
