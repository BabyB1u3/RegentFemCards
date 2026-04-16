# StS2 Portrait Mod Generator

This repository is being reshaped from a single portrait-replacement mod into a reusable toolchain for generating new Slay the Spire 2 portrait replacement mods.

## What is in the repo

- `archive/RegentFemCardsReference/`
  Reference implementation of an existing portrait replacement mod.
- `templates/PortraitReplacementTemplate/`
  Clean reusable template for generated mods.
- `data/official_card_index.json`
  Pre-generated official release-card index used as baseline matching data.
- `tools/PortraitModGenerator.Core/`
  Core library for template-based mod generation.
- `tools/PortraitModGenerator.Cli/`
  CLI entry point for import, scan, and generation workflows.
- `docs/MOD_GENERATOR_DESIGN.md`
  Detailed design document for the generator architecture.
- `docs/OFFICIAL_CARD_INDEX.md`
  Notes about the built-in official card index dataset.

## Current direction

The long-term goal is:

1. Import one or more `.pck` files with GDRETools headless CLI.
2. Extract and scan image assets.
3. Normalize names and let the user confirm mappings.
4. Generate a new mod project from the template.
5. Fill `card_replacements.json` and copy selected portraits.
6. Build the generated mod.

## Current status

Already done:

- Extracted a reusable `PortraitReplacementTemplate`.
- Cleaned the template so it no longer carries the current mod's real portrait assets or mappings.
- Added `PortraitModGenerator.Core` with template generation, GDRE recover import, and asset scanning.
- Added `PortraitModGenerator.Cli` with `generate-template`, `import-pck`, and `scan-assets`.
- Added a pre-generated `official_card_index.json` baseline for authoritative card ids.

Not done yet:

- Matching imported assets against the official card index
- Candidate mapping generation and review flow
- Mapping editor UI
- End-to-end build pipeline for generated mods

## Build notes

The reference mod and the generator core are separate concerns:

- The reference mod is tied to the Slay the Spire 2 / Godot mod environment.
- The generator core is a standalone tooling project.

In the current environment, `PortraitModGenerator.Core` targets `net10.0` so it can build with the locally available SDK/reference packs.

## Repository hygiene

Generated and local-only content should not be committed:

- `.dotnet_cli/`
- `bin/`
- `obj/`
- IDE caches

The template under `templates/PortraitReplacementTemplate/` is intended to stay clean and minimal.
