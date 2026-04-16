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
- Beta-only portraits are excluded on purpose so later matching logic stays focused on the release game.
- Future mapping analysis should match imported assets against this index instead of guessing ids in isolation.
