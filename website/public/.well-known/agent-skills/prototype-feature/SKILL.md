---
name: prototype-feature
description: >-
  Build a clickable local-only frontend prototype from a plain-English feature
  request, with realistic mock data and action hooks shaped for later GraphQL
  mutations. Use when the user asks to prototype, mock up, wireframe, sketch,
  or build UI before backend/schema work exists ("no backend yet", "no
  GraphQL yet", "just the UI"). Do not use when wiring a real backend or
  designing the GraphQL contract now.
---

# Prototype feature

Build the feature as real UI with local data. Preserve component and action boundaries so `prototype-to-contract` can replace mocks with fragments and mutations later.

## Read

- Read the existing app shell, routes, shared components, design tokens, data conventions, and state patterns.
- Fit the app. Ask before introducing a new dependency, design convention, route pattern, or state model.

## Build

- Build only the requested workflow. Do not invent extra product surface.
- Keep data local. Do not add backend calls, schema files, generated GraphQL artifacts, or async loading simulation.
- Make the prototype clickable with local state. Use the repo's existing frontend stack and conventions.

## Data

- Model data as product-domain entities with stable ids, nested relationships, good names, and computed fields when useful.
- Seed enough realistic records to cover every reachable state needed to review the workflow.

## Components

- Give each component one primary entity.
- Split when a component reads fields from two entities.
- Pass related entities to child components as objects; do not read their fields in the parent.

## Actions

- Model each user action as a local hook named like the future mutation hook, such as `useCreateReviewMutation` and put it in a new file.
- Return the usual mutation tuple: `[action, { loading, error }]`.
- Let handlers call the action function now. Keep the backend TODO at the hook implementation boundary.
- Pass what you need for local state management into the hook.

## Example

```tsx
type Author = { id: string; name: string; initials: string };
type Review = {
  id: string;
  body: string;
  author: Author;
  viewerCanEdit: boolean;
};

interface ReviewCard {
  review: Review;
}

function ReviewCard({ review }: ReviewCard) {
  return (
    <article>
      <p>{review.body}</p>
      <UserChip user={review.author} />
    </article>
  );
}

function ReviewForm() {
  const [createReview, { loading, error }] = useCreateReviewMutation();
  return (
    <>
      <button disabled={loading} onClick={() => createReview({ body: "..." })}>
        Post
      </button>
      {error && <p role="alert">{error.message}</p>}
    </>
  );
}
```
