# Agent logos

Drop the official agent brand SVGs here to replace the placeholder glyphs in the
agentic-coding section ("Built for [agent]" take).

Expected files (one per agent, from each vendor's official brand / press kit):

- `claude.svg`
- `codex.svg`
- `copilot.svg`
- `cursor.svg`
- `windsurf.svg`
- `gemini.svg`
- `aider.svg`
- `cline.svg`

Prefer a monochrome or light variant so it reads on the dark navy surface. Once a
file is present, set `logoSrc` for that agent in
`src/components/home/agentic/AgenticSectionV3.tsx`; until then the section shows a
neutral placeholder glyph (not a reproduction of the real brand mark).
