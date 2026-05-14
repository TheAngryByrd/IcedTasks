---
name: grill-me-docs
description: "Use when creating, auditing, or updating project documentation. First inspect existing docs, code, README files, Microsoft knowledge base results, and marketing pages; then interview the user one targeted question at a time to fill gaps; finally produce PR-ready documentation, glossary updates, and a summary of assumptions, sources, and unresolved questions."
---

Interview me relentlessly. Ask one high-value question at a time, only after checking available sources first. Stop asking when there is enough evidence to produce useful documentation, and clearly list any remaining unknowns.

## Documentation awareness

Documentation is often stored in:

- `docsSrc/`
- `.docs/`
- Root `README.md`
- Directory-specific `README.md` files
- DocComments in code

## Source priority

When sources disagree, use this priority order unless the user says otherwise:

1. Current code behavior
2. Product owner/user clarification
3. Existing internal documentation
4. External Microsoft documentation for specific technologies or patterns

Never invent undocumented behavior. Mark uncertain claims as assumptions or ask a targeted question.

## Glossary format

For each term in `GLOSSARY.md`, include:

- Canonical term
- Definition
- User-facing aliases
- Internal/code aliases
- Related code concepts, models, tables, or routes
- Example usage
- Common confusion or non-examples

## Workflow

1. Determine documentation intent
    - New docs, update existing docs, glossary work, audit, or cleanup.
    - The core audience is always developers, but sometimes there are secondary audiences like customer support or AI agents that should be considered.

2. Inventory existing documentation
    - Search `docs/`, `.docs/`, root `README.md`, package/module `README.md` files, MDX files, help text, route-level docs, and comments when relevant.
    - Identify duplicate, stale, missing, or contradictory docs.

3. Search external product sources before asking
    - F# Documentation (https://learn.microsoft.com/en-us/dotnet/fsharp/).
    - F# Core Library Documentation (https://fsharp.github.io/fsharp-core-docs/)
    - TPL Documentation (https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-based-asynchronous-programming)
    - Prefer primary project/code/docs evidence over assumptions.

4. Cross-reference with code
    - Inspect routes, controllers, models, actors, schemas, API clients, tests, fixtures, and UI copy relevant to the topic.
    - If code and user explanation conflict, pause and ask which source is authoritative.

5. Build an interview queue
    - Ask one targeted question at a time.
    - Prioritize blockers that affect accuracy, terminology, audience, or user workflow.
    - Avoid questions already answered by docs, code, KB, or marketing copy.

6. Interview rules
    - Ask one question at a time and wait for the answer.
    - Be specific in your questions and give simple response options when possible.
    - Try to answer the question yourself first using available sources including code, then ask the user to confirm or clarify.
    - Always allow the user to correct you if your assumptions or options are wrong.
    - When the user uses a term, ask them to define it and provide an example of how it's used in the context of the project.
    - Stop asking when there is enough information to make useful documentation changes.

7. Produce documentation changes
    - Update or create the appropriate docs.
    - Update `GLOSSARY.mdx` when terms are clarified.
    - Add/Update DocComments when relevant.
    - Include examples, edge cases, and links to related docs.
