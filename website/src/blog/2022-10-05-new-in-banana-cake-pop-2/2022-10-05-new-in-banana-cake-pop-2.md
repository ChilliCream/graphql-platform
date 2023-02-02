---
path: "/blog/2022/10/05/new-in-banana-cake-pop-2"
date: "2022-10-05"
title: "New in Banana Cake Pop 2"
description: "Drag & drop support for documents, GraphQL defer/stream support, GraphQL operation extraction, and many further improvements."
tags: ["bananacakepop", "graphql", "ide", "cloud", "release"]
featuredImage: "new-in-banana-cake-pop-2.png"
featuredVideoId: X3cfnm0UHVQ
author: Rafael Staib
authorUrl: https://github.com/rstaib
authorImageUrl: https://avatars0.githubusercontent.com/u/4325318?s=100&v=4
---

## Getting started

Everything you need to get started with **Banana Cake Pop** you'll find on [bananacakepop.com](https://bananacakepop.com)

## Drag & Drop

We've added _Drag & Drop_ support to the document explorer. That makes moving documents, files and folders around so much easier. Moreover, we've added support for dropping files for _File Upload_ from outside. Just drag one or multiple files (e.g. a photo) from your computer and drop it on the document explorer root or a specific folder.

## Defer/Stream Spec

We've updated to the latest Defer/Stream spec draft version, but with backward-compatibility in mind. It still works with previous versions of Hot Chocolate or other servers that implemented the prior spec version.

## Operation Extraction

We've optimized how GraphQL operations are sent over the wire. Before we send an operation, we remove all the superfluous operations and fragments. For instance, if we have two queries in a GraphQL document, query `A` and `B`, we send only the query and its fragments we execute. Such a document could look like the following.

```graphql
query A {
  me {
    ...UserFragment
  }
}

query B {
  me {
    ...UserFragment
    friends {
      ...FriendFragment
    }
  }
}

fragment UserFragment on User {
  name
  image
}

fragment FriendFragment on User {
  ...User
  age
}
```

If we execute query `A`, for example, the request would look like the following.

```graphql
query A {
  me {
    ...UserFragment
  }
}

fragment UserFragment on User {
  name
  image
}
```

With this technique, we didn't only reduce the request overhead but were also able to send query `A` even though query `B` is not valid.

## Horizontal Scrolling for Tabs

We've added horizontal scrolling on tabs for mice with a scroll wheel. Instead of scrolling up/down, we switched to scrolling left/right. Simply hover over tabs that contain partly visible tabs and use the scroll wheel of the mouse to move hidden tabs into the visible area.

## Insider Version

We start now with insider versions for the Electron app, which will run side-by-side with the released app version. Follow this link [bananacakepop.com](https://bananacakepop.com) to download the first insider build.

## Further Improvements

A few more, worth mentioning, improvements are listed below.

1. Increased efficiency of workspace synchronization
1. Reduced Electron app size
1. Removed app leave warning prompt
1. Increased editor performance

## Subscribe

To stay up to date, subscribe to our [ChilliCream YouTube Channel](https://www.youtube.com/c/ChilliCream) to get notified whenever we publish new videos.

I'm Rafael Staib, and as soon as **Banana Cake Pop 3** is released, I'll be right here to tell you what's new in **Banana Cake Pop**!
