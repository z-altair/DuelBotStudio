# DuelBot Studio

DuelBot Studio is a Windows desktop application for designing Yu-Gi-Oh! bots and authoring their declarative behavior as versioned JSON. It also retains separate card-search and deck-editing tools. It is built with WPF on .NET 10.

## Requirements

- Windows
- .NET 10 SDK
- Internet access for uncached card data and images

## Build and test

Run these commands from the repository root:

```powershell
dotnet restore DuelBotStudio.slnx
dotnet build DuelBotStudio.slnx
dotnet test DuelBotStudio.slnx
```

Run the application from Visual Studio or with:

```powershell
dotnet run --project DuelBotStudio/DuelBotStudio.csproj
```

## Current features

- Create bot profiles with named strategy modes, ordered decision rules, typed conditions, actions, and optional mode transitions.
- Search for cards and keep their stable database IDs as rule targets or condition references.
- Validate and import/export versioned `.json` bot profiles; unsupported schema versions and invalid references are reported rather than silently accepted.
- Import `.ydk` files with `#main`, `#extra`, and `!side` sections.
- Accept the conventional `#created by` metadata line.
- Report malformed lines and unknown card IDs with their source line numbers. An invalid file does not replace the active deck.
- Search cards by name and description using the YGOPRODeck API.
- Add cards explicitly to the main, extra, or side deck and remove cards from the deck viewer.
- Export decks as `.ydk` files and prompt before discarding unsaved changes.
- Validate main/extra/side section sizes and total copies against configurable `DeckRules`. Defaults are 40–60 main-deck cards, up to 15 extra-deck cards, up to 15 side-deck cards, and up to 3 copies of a card across the deck.
- No banlist is bundled or enforced. Applications embedding the deck manager can supply their own structural limits.

## External services and cache

Card metadata is retrieved from the YGOPRODeck API; card images are retrieved from its image service. Successful metadata responses and downloaded images are cached under the following directory. In-memory caches are bounded, and duplicate concurrent requests for the same card share a single fetch:

```text
%LOCALAPPDATA%\DuelBotStudio\Cache
```

Metadata is stored in `Data` and images in `Images` below that directory.

Invalid cached metadata and corrupt cached images are removed and fetched again. Network, JSON, and cache-access failures are reported to the UI instead of being presented as empty search results.

## Bot profiles

Bot profiles are independent from deck documents. The JSON schema currently has version `1`; enum values are serialized as stable camel-case strings. Each rule specifies a decision point, a mode, an action, a priority, zero or more conditions (combined with AND), and an optional next mode. Higher priority values are intended to be considered first by a compatible runtime.

For example, a rule may request a normal summon for a card in hand and switch to a defensive mode after it matches:

```json
{
  "schemaVersion": 1,
  "botName": "Example bot",
  "initialModeId": "combo",
  "modes": [
    { "id": "combo", "name": "Combo" },
    { "id": "defensive", "name": "Defensive" }
  ],
  "rules": [
    {
      "id": "summon-starter",
      "name": "Summon starter",
      "modeId": "combo",
      "decisionPoint": "mainPhaseCommand",
      "conditions": [
        {
          "type": "cardInZone",
          "cardId": 123456,
          "cardName": "Example card",
          "zone": "hand",
          "minimumCount": null
        }
      ],
      "action": "normalSummon",
      "targetCardId": 123456,
      "targetCardName": "Example card",
      "priority": 100,
      "nextModeId": "defensive",
      "isEnabled": true
    }
  ]
}
```

The editor can describe conditions such as a card being in a zone, a chain response being available, an effect being negated, and an effect-use count reaching a threshold. DuelBotStudio validates the profile structure but does not execute these conditions or play a duel. In particular, effect-negation history and effect-use counters require support from the eventual runtime. The Wrapper is not a project dependency and is not modified by this application.

To create a profile, open **Bot editor**, add or select a rule, choose its decision point/action/mode, add any conditions, and apply the rule. Use card search to set an action target or a card-specific condition, then save the profile as JSON. Loading validates the document before replacing the current profile.

The visual flow-diagram editor and dynamic rule execution are future work. The current rules and mode transitions are the serialized model that such a diagram can edit later.

## Architecture

- `Models/`: card, deck, import-result, and bot profile/rule data types.
- `Services/`: YDK parsing, deck editing and validation, card metadata, and image caching.
- `ViewModels/`: workspace orchestration, card-search/editor state, and bot profile editing.
- `Views/`: WPF windows and deck/bot editor controls.
- `DuelBotStudio.Tests/`: parser, deck validation, bot profile schema/validation/persistence, editor state, YDK export, and cache tests.

Public C# APIs are documented with XML comments. Generated XML documentation is emitted alongside the application assembly.
