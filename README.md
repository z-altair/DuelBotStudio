# DuelBot Studio

DuelBot Studio is a Windows desktop application for searching Yu-Gi-Oh! cards, importing and editing decks, and viewing deck contents. It is built with WPF on .NET 10.

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

- Import `.ydk` files with `#main`, `#extra`, and `!side` sections.
- Accept the conventional `#created by` metadata line.
- Report malformed lines and unknown card IDs with their source line numbers. An invalid file does not replace the active deck.
- Search cards by name and description using the YGOPRODeck API.
- Add cards explicitly to the main, extra, or side deck and remove cards from the deck viewer.
- Validate main/extra/side section sizes and total copies against configurable `DeckRules`. Defaults are 40–60 main-deck cards, up to 15 extra-deck cards, up to 15 side-deck cards, and up to 3 copies of a card across the deck.
- No banlist is bundled or enforced. Applications embedding the deck manager can supply their own structural limits.

## External services and cache

Card metadata is retrieved from the YGOPRODeck API; card images are retrieved from its image service. Successful metadata responses and downloaded images are cached under:

```text
%LOCALAPPDATA%\DuelBotStudio\Cache
```

Invalid cached metadata and corrupt cached images are removed and fetched again. Network, JSON, and cache-access failures are reported to the UI instead of being presented as empty search results.

## Architecture

- `Models/`: card, deck, import-result, and reserved bot data types.
- `Services/`: YDK parsing, deck editing and validation, card metadata, and image caching.
- `ViewModels/`: workspace orchestration and card-search/editor state.
- `Views/`: WPF windows and controls.
- `DuelBotStudio.Tests/`: parser, deck validation, transactional import, and metadata-cache tests.

Public C# APIs are documented with XML comments. Generated XML documentation is emitted alongside the application assembly.

## Bot functionality

Bot behavior is intentionally out of scope. The current bot types are unused placeholders; there is no bot editor, condition language, action executor, or duel-engine integration. Those contracts should be designed before bot functionality is implemented.
