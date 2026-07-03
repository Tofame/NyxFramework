# AGENTS.md — NyxDrawer

This project bridges `NyxAssets` (game sprites, outfits, items) with `NyxRender` (GPU drawing) to composite and draw game objects.

## Architecture & Responsibilities

- **AssetDrawer**: The main entry point for compositing and drawing items, creatures, outfits, missiles, and spell effects.
- **Outfit Compositor**: Handles overlaying head, body, legs, and feet outfit layers, applying custom color lookups and masking.
- **Direction & Animation**: Dictates animation framing, movement transitions, and sprite offsets based on direction and walk cycle durations.

## Key APIs & Models

- `AssetDrawer.DrawCreature(...)`: Composite and draw creature outfits.
- `AssetDrawer.DrawItem(...)`: Draw ground/container items.
- `OutfitColorLayout`: Struct defining custom head/body/legs/feet palette layout translations.

## Development Guidelines

- **Decoupling**: Relies on constructor injection of `ClientAssetBundle` and `SpriteRenderer`. Do not hardcode assets pathing or engine references.
