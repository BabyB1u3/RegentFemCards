# Official Card Index

`data/official_card_index.json` is a pre-generated baseline dataset for all known replaceable Slay the Spire 2 card portraits.

It is intentionally treated as repository data, not as part of the end-user import flow. The generator should load this file directly instead of rescanning the official unpack every time.

This dataset is intentionally scoped to the official non-beta card pool only.

## Format

```json
{
  "version": "sts2-official-card-index-v1",
  "cards": [
    {
      "cardId": "arsenal",
      "canonicalName": "Arsenal",
      "group": "regent"
    }
  ]
}
```

## Notes

- `cardId` is the normalized stable id used for matching.
- `canonicalName` is a display-friendly PascalCase form derived from the official filename.
- `group` corresponds to the official portrait folder group such as `regent`, `ironclad`, or `colorless`.

## Current usage

- This file is the authoritative source for candidate card ids.
- Beta-only portraits are excluded on purpose so the matching logic stays focused on the release game.
- [MappingAnalyzer](../tools/PortraitModGenerator.Core/Services/MappingAnalyzer.cs) loads this index via [OfficialCardIndexLoader](../tools/PortraitModGenerator.Core/Services/OfficialCardIndexLoader.cs) and uses it for deterministic asset-to-card matching during the analyze stage.
- The merge stage groups candidates by the `cardId` produced from this index; entries with the same `cardId` from multiple `.pck` packages become a conflict group in the GUI.
