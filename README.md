# SitAndFish 🎣🪑

A [SMAPI](https://smapi.io/) mod for [Stardew Valley](https://www.stardewvalley.net/) that lets you **fish while sitting on a chair**.

![Sitting and Fishing](screenshot.png)

## Features

- Sit on any chair or bench near water
- Cast your fishing rod **while sitting**
- The sitting state is preserved during fishing
- Works with all fishing rods (Bamboo, Fiberglass, Iridium)

## Requirements

|                | Version |
| -------------- | ------- |
| Stardew Valley | 1.6.14+ |
| SMAPI          | 4.0.0+  |

## Installation

1. Install [SMAPI](https://smapi.io/)
2. Download the latest release from [Releases](../../releases)
3. Extract `SitAndFish` folder into your `Mods` directory
4. Launch the game with SMAPI

## How It Works

The mod uses [Harmony](https://harmony.pardeike.net/) to patch `Game1.pressUseToolButton`. When the player is sitting and holding a fishing rod, the mod temporarily bypasses the vanilla sitting restriction to allow casting.

## Building from Source

```bash
cd SitAndFish
dotnet build
```

> **Note:** Update the DLL references in `SitAndFish.csproj` to match your Stardew Valley installation path.

## License

MIT
