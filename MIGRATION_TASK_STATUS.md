# Migration Task Status

Branch: tte/graphql-dotnet-migratoin-guide   (decision: committing here; not main, branch policy satisfied)
Last updated: 2026-06-19

## Phase 0 — Setup            [x]
## Phase 1 — Research         [x]  (7 research-*.md + feature-map.md in .work/migration/)
## Phase 2 — GraphQL.NET app  [x]   commit: 5c0eaa57be
## Phase 3 — Migration guide  [x]   commit: ad5f9ddf8b (registered in docs.json line 772; accuracy-reviewed clean; no em-dashes)
## Phase 4 — Port + verify    [x]   commit: 2269ea46db (app done+verified)
  - HC port built+ran (5102), Q1-Q5 + P1/P2 node data match before app byte-for-byte.
  - Intentional documented diffs: mutation conventions payload shape, auth error code, cursor encoding, ID/UUID scalars.
  - after-results.md + before-results.md hold captured outputs.
## Phase 5 — Final review     [x]
  - Both example apps build clean (net10.0). Guide registered + accuracy-reviewed (no corrections, no em-dashes).
  - docs.json valid JSON; entry at line 772 (v16 migrating, first item).
  - Commits on branch tte/graphql-dotnet-migratoin-guide: 5c0eaa57be, 2269ea46db, ad5f9ddf8b.
  - Note: .work/migration/ artifacts are gitignored (local only), as is examples bin/obj.

### House style notes (reference guides)
- Frontmatter: `---\ntitle: ...\n---`
- `# Breaking changes` style h1, `##` per concept.
- Before/after via bold `**Before**` / `**After**` labels above fenced code blocks; `diff` blocks for changes.
- No em-dashes. Tables for API mapping summaries.
- Sidebar: website/src/docs/docs.json, v16 migrating items array (~line 770).

### Notes / blockers
- Reference apps: examples/migration/graphql-dotnet-to-hotchocolate/{before-graphql-dotnet,after-hotchocolate}
- Do NOT add to src/All.slnx unless asked.

### Versions (verified)
- GraphQL.NET: GraphQL 8.8.4
- Hot Chocolate: HotChocolate.AspNetCore 16.2.1

### Reference app feature checklist (both apps, Books/Authors domain)
1. Query object types + nested list (books { title author { name } })
2. Scalars + enum (String/Int/ID/DateTime/UUID + BookGenre)
3. Union SearchResult = Book | Author (and/or Node interface)
4. Arguments + input object (books(filter: BookFilterInput))
5. Mutation addBook (HC mutation conventions + typed [Error])
6. Subscription onBookAdded (provider registered, websockets)
7. Batch DataLoader authors-for-books (N+1 fix)
8. Cursor pagination on a list (books first/after)
9. Authorization on one field
10. UI endpoint + POST /graphql

### Test operations (equivalence set)
- Q1: { books { id title genre publishedYear author { id name } } }
- Q2: { books(filter: { genre: FANTASY }) { title genre } }
- Q3: { bookById(id: "1") { title author { name } } }
- Q4: { authors { name books { title } } }
- Q5: { search(term: "a") { __typename ... on Book { title } ... on Author { name } } }
- M1: mutation { addBook(...) { id title author { name } } }  (HC shape differs via mutation conventions - documented)
- Q6: { secret } without creds -> authorization error; with X-Authenticated header -> value
- GraphQL.NET server: http://localhost:5101  (/graphql, /ui/graphiql)
- Captured: .work/migration/before-results.md

### Commits
- TBD
