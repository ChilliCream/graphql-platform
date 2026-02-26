# Docs Writer

Use these instructions whenever generating documentation for an open-source project. The goal is not to document your software — it's to **make the reader awesome** at what your software enables them to do.

A reader who finishes your docs should feel more competent at building software, not just more familiar with your API surface. Frame everything around what the reader will accomplish, not what the software can do.

---

## 1. STRUCTURE

### Organize by problem domain, not information type

Readers search by _what they're trying to do_ — "pagination", "authentication", "subscriptions" — not by document category. A developer with a pagination problem doesn't know whether the answer is in "Concepts" or "Reference"; they know they need pagination.

Organize the left sidebar around **topics and tasks**, not around Diátaxis categories. Within each topic page, use distinct writing modes (tutorial, how-to, reference, explanation) as section-level tools.

### Four writing modes

Use these as modes within a page, not as page categories:

| Mode             | Purpose                       | User State                | Style                                                             |
| ---------------- | ----------------------------- | ------------------------- | ----------------------------------------------------------------- |
| **Tutorial**     | Learning by doing             | Studying (beginner)       | Hand-holding, step-by-step, "let's build X together"              |
| **How-To Guide** | Solve a specific problem      | Working (competent user)  | Goal-oriented, concise steps, assumes baseline knowledge          |
| **Reference**    | Look up technical facts       | Working (needs specifics) | Dry, precise, complete, structured like the codebase itself       |
| **Explanation**  | Understand concepts/decisions | Studying (wants depth)    | Discursive, contextual, "here's why", like a knowledgeable friend |

These are not the only section types. **Troubleshooting** sections (common errors, what they mean, how to fix them) are equally important and belong on any page where readers are likely to get stuck. Reference sections benefit from a **Usage** subsection showing practical recipes alongside the formal API signatures.

### One topic, one page

Prefer **comprehensive single pages** over splitting a topic across multiple pages. The site has dual sidebar navigation — a left sidebar for topic navigation and a right sidebar for in-page section navigation. Long, well-structured pages are easy to navigate. A reader exploring "Subscriptions" should find everything about subscriptions on one page, not hunt across three.

A page can flow from tutorial → how-to → reference → explanation as long as each section is clearly labeled and **stays true to its mode within that section**. The reader uses the right sidebar to jump directly to what they need.

### Don't blend modes within a section

The rule is about not mixing writing modes _within a paragraph or section_ — not about forcing separate pages. What to avoid: a tutorial step that suddenly drops into a wall of API parameter tables, or a reference list that starts narratively explaining design philosophy mid-row. When modes blur at the sentence level, the reader loses track of what they're supposed to do with the information.

Within a section, commit to one mode. If a tutorial section needs to explain _why_, keep it to a sentence and link to the explanation section on the same page. If a reference section needs to show _how to_, cross-reference the how-to section above or below.

### Repetition is OK

Documentation is not code — don't apply DRY. If a concept needs to be restated on a different page for that page to make sense, restate it. A reader landing on any page via search should be able to understand it without reading three other pages first. Partial coverage that sends readers on a scavenger hunt is worse than some repetition.

---

## 2. MENTAL MODELS & TEACHING

### Teach how to think about it, not just how to use it

The most effective documentation builds accurate **mental models** — internal representations of how a system works. A reader who understands the execution pipeline _conceptually_ can figure out specifics on their own. A reader who memorizes API signatures cannot.

Organize learning content by **conceptual progression**, not by API surface. Explain the "why" and the "how it works" — then the "what" (specific APIs) makes intuitive sense.

### Start from where readers get confused

Don't start from what the technology _is_ — start from where readers are likely confused or hold misconceptions. If developers routinely misuse a feature, document the correct mental model first. A page titled "You Might Not Need X" that corrects a common mistake is more valuable than a page that describes X's API.

### Connect new concepts to existing knowledge

Explicitly bridge from what the reader already knows: "If you've used middleware in Express, Hot Chocolate's request pipeline works similarly." This activates prior knowledge and reduces cognitive load. Never introduce a concept in isolation when an analogy to something familiar would anchor it.

### Serve beginners and experts on the same page

Research shows that instructional techniques effective for novices can actively hurt experienced readers (and vice versa). The solution is **progressive disclosure with adaptive fading**: start each section with the simplest version, then layer in complexity. Use expandable sections or clearly marked "deep dive" subsections for advanced details. An expert scanning the right sidebar can jump straight to what they need; a beginner reads top-to-bottom.

---

## 3. LANGUAGE & VOICE

### Tone

- **Conversational but not cutesy.** Write like a smart colleague explaining something at a whiteboard — not a textbook, not a marketing page.
- **Confident but not arrogant.** State things directly. Avoid hedging ("you might want to perhaps consider...").
- **Friendly but not fluffy.** Zero filler. Every sentence earns its place.

### Person & Voice

- **Use second person ("you").** Write directly to the reader: "You can configure this by..." not "The user can configure this by..."
- **Use active voice.** "Run the migration" not "The migration should be run."
- **Use present tense.** "This returns a list" not "This will return a list."
- **Use imperative mood for instructions.** "Install the package" not "You should install the package."
- **Conditional before instruction.** "To enable caching, add the following middleware" not "Add the following middleware to enable caching." Lead with the _why_, then the _what_.

### Clarity Rules

- **Short sentences.** If a sentence has a comma and an "and" and another clause — split it.
- **One idea per paragraph.** 2–4 sentences max.
- **Plain language.** "Use" not "utilize". "Start" not "initiate". "About" not "approximately".
- **Define jargon on first use** or link to a glossary. Never assume the reader knows your internal terminology.
- **Be precise with words.** Don't say "simply" or "just" (it's dismissive if the reader is struggling). Don't say "easy" or "obvious." These words are red flags for the curse of knowledge — if the reader _is_ struggling, they now feel stupid too.
- **Front-load everything.** Put the most important word first in headings, paragraphs, and list items. Readers scan the first 2–3 words of each element and skip the rest. "Configure authentication for endpoints" not "A guide to configuring authentication."
- **Write for a global audience.** Avoid idioms, culturally specific references, and humor that doesn't translate. Readers come from everywhere.

---

## 4. FORMATTING FOR SCANNABILITY

Developers scan — they don't read linearly. Eye-tracking research shows they jump to code blocks first, scan headings second, and read prose only when something is unclear. Format for this behavior:

- **Headings are navigation.** Make them descriptive and task-oriented: "Configure the database connection" not "Configuration". The right sidebar builds its table of contents from your headings — make every heading useful for jumping to.
- **Lead with code, follow with explanation.** Developers read code examples first and refer to surrounding text only when something is ambiguous. Show a working example, then explain what it does — not the other way around.
- **Code examples must be:**
  - **Copy-pasteable and runnable.** Every code snippet a reader encounters will be copied. Even examples labeled "don't do this" will be copied. Make every example production-quality.
  - **Minimal** — only the relevant parts.
  - **Annotated** with brief inline comments where non-obvious. Integrate explanations directly alongside code rather than in a separate paragraph below — this prevents the reader from splitting attention between two locations.
  - **Showing expected output** so the reader can verify they're on track.
- **Multi-representation tabs** — When a concept can be shown from multiple angles (e.g., SDL schema vs. C# implementation vs. resulting GraphQL), use tabbed code blocks so the reader picks their preferred view.
- **Use tables** for option/parameter lists, comparisons, and multi-dimensional info.
- **Use admonitions/callouts** sparingly for: warnings (will break things), tips (non-obvious shortcuts), notes (important context). Don't overuse — if everything is highlighted, nothing is.
- **Use visual aids** — Mermaid diagrams, screenshots, and architecture diagrams where they clarify flow or structure better than prose can. A diagram of a request pipeline beats three paragraphs of description. But don't restate in prose what a clear diagram already shows — redundant explanation adds cognitive load, not clarity.
- **Link liberally.** Connect related concepts. Don't make the reader search for the next thing. Link between sections on the same page as well as across pages.
- **Keep pages focused.** One topic per page. Long pages are fine — unfocused pages are not.

---

## 5. ENGAGEMENT & READER EXPERIENCE

### Make the Reader Feel Competent

- Start every tutorial with what they'll achieve: "By the end of this guide, you'll have a running API that..."
- Provide checkpoints: "If everything worked, you should see..."
- Show expected output after code blocks so the reader can verify they're on track.

### Reduce Friction

- Never make assumptions about environment without stating them. List prerequisites upfront.
- Provide copy-paste commands. Include the full command, not "run the install command."
- Handle the unhappy path: common errors, what they mean, how to fix them. If a step can fail, tell the reader what failure looks like and how to recover.

### Create Flow

- End sections with a bridge to the next: "Now that auth is configured, let's set up the database."
- End pages with "Next steps" — give the reader a clear path forward, not a dead end.
- Use progressive disclosure: start simple, layer in complexity. Don't front-load every edge case.

### Show, Don't Lecture

- Lead with a code example, then explain it — not the other way around.
- Use real-world scenarios, not abstract descriptions: "Suppose you're building a chat app and need real-time updates..." not "The WebSocket module facilitates bidirectional communication."
- Before/after examples are powerful for showing the value of a feature.

---

## 6. CONSISTENCY RULES

- **Terminology:** Pick one term for each concept and use it everywhere. Don't alternate between "token", "key", and "credential" for the same thing.
- **Formatting patterns:** If one CLI flag is documented as `--flag=value`, all flags must use that format.
- **Code style:** Match the project's actual code style in examples.
- **Capitalization:** Be consistent with product names, feature names, and headings.

---

## 7. QUICK CHECKLIST BEFORE FINALIZING

- [ ] Can a new user go from zero to "it works" in under 5 minutes with the quickstart?
- [ ] Does every page answer one clear question?
- [ ] Are all code examples tested and runnable?
- [ ] Does every code example show expected output?
- [ ] Is there a clear next step at the end of every page?
- [ ] Are prerequisites stated upfront?
- [ ] Are error cases and troubleshooting covered?
- [ ] Is the language free of "just", "simply", "easy"?
- [ ] Would a non-native English speaker understand this clearly?
- [ ] Are all internal links working and pointing to the right sections?
- [ ] Would a beginner understand this page without reading three other pages first?
- [ ] Can an expert find what they need via the right sidebar without reading the beginner content?

---

## 8. SECTION-TYPE SPECIFIC RULES

### Tutorials

- Take the reader through ONE complete, working example end-to-end.
- You are responsible for the reader's success. Leave nothing ambiguous.
- Explain only what's needed in the moment. Link to explanations for depth.
- The reader should feel accomplishment at the end.

### How-To Guides

- Title format: "How to [verb] [thing]" — e.g., "How to configure CORS".
- Assume competence. Don't re-explain basics.
- Numbered steps. Keep them atomic (one action per step).
- Include gotchas and edge cases inline.

### Reference

- Structure mirrors the codebase (API endpoints, classes, config keys).
- Every parameter: name, type, default, description, example.
- No opinions, no tutorials, no narrative. Just accurate, complete facts.
- Keep it up to date with the code — stale reference docs destroy trust.
- Add a **Usage** subsection with practical recipes showing _how and why_ you'd use each API in context. This complements the formal signatures without replacing them.

### Explanation

- Answer "why" and "how does this work under the hood."
- Use analogies, diagrams, comparisons to similar tools.
- It's okay to be opinionated here. Share design rationale and trade-offs.
- Can be longer and more discursive than other types.

### Troubleshooting

- Structure as problem → cause → solution.
- Use the exact error message as the heading when possible — readers will search for it.
- Cover the 3–5 most common issues. Don't try to be exhaustive.
- Link to deeper explanations where the root cause is non-obvious.
