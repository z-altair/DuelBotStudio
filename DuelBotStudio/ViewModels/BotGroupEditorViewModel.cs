using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using DuelBotStudio.Models.Bot;
using DuelBotStudio.Models.Card;
using DuelBotStudio.Services;

namespace DuelBotStudio.ViewModels
{
    /// <summary>Edits reusable card groups independently from bot profiles.</summary>
    public sealed class BotGroupEditorViewModel : ObservableObject
    {
        private readonly YgoCardInfoService _cardInfoService;
        private readonly BotGroupLibraryJsonService _libraryService;
        private BotCardGroup? _selectedGroup;
        private CardModel? _selectedCard;
        private CardModel? _selectedGroupCard;
        private string _searchQuery = string.Empty;
        private string _statusMessage = string.Empty;
        private string? _filePath;

        /// <summary>Creates the group editor.</summary>
        public BotGroupEditorViewModel(
            YgoCardInfoService cardInfoService,
            BotGroupLibraryJsonService libraryService)
        {
            _cardInfoService = cardInfoService;
            _libraryService = libraryService;
            SearchResults.CollectionChanged += (_, _) => OnPropertyChanged(nameof(SearchResults));
        }

        /// <summary>Gets reusable groups.</summary>
        public ObservableCollection<BotCardGroup> Groups { get; } = new();

        /// <summary>Gets cards in the selected group.</summary>
        public ObservableCollection<CardModel> GroupCards { get; } = new();

        /// <summary>Gets card search results.</summary>
        public ObservableCollection<CardModel> SearchResults { get; } = new();

        /// <summary>Gets or sets the selected group.</summary>
        public BotCardGroup? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                if (SetProperty(ref _selectedGroup, value))
                {
                    _ = RefreshGroupCardsAsync();
                }
            }
        }

        /// <summary>Gets or sets the selected search result.</summary>
        public CardModel? SelectedCard
        {
            get => _selectedCard;
            set => SetProperty(ref _selectedCard, value);
        }

        /// <summary>Gets or sets the selected card in the active group.</summary>
        public CardModel? SelectedGroupCard
        {
            get => _selectedGroupCard;
            set => SetProperty(ref _selectedGroupCard, value);
        }

        /// <summary>Gets or sets the search text.</summary>
        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        /// <summary>Gets the current status text.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>Gets the loaded or saved library path.</summary>
        public string? FilePath
        {
            get => _filePath;
            private set => SetProperty(ref _filePath, value);
        }

        /// <summary>Creates a new empty group.</summary>
        public void CreateGroup()
        {
            var group = new BotCardGroup
            {
                Id = $"group-{Guid.NewGuid():N}",
                Name = "New group"
            };
            Groups.Add(group);
            SelectedGroup = group;
            StatusMessage = "New group created. Give it a name and add cards.";
        }

        /// <summary>Removes the selected group.</summary>
        public void RemoveSelectedGroup()
        {
            if (SelectedGroup is null)
            {
                StatusMessage = "Select a group first.";
                return;
            }

            string name = SelectedGroup.Name;
            Groups.Remove(SelectedGroup);
            SelectedGroup = Groups.FirstOrDefault();
            StatusMessage = $"Group '{name}' removed.";
        }

        /// <summary>Searches the card database.</summary>
        public async Task SearchCardsAsync()
        {
            if (SearchQuery.Trim().Length < 3)
            {
                StatusMessage = "Enter at least three characters to search.";
                return;
            }

            try
            {
                SearchResults.Clear();
                foreach (CardModel card in await _cardInfoService.SearchCardsByNameAsync(SearchQuery))
                {
                    SearchResults.Add(card);
                }

                StatusMessage = SearchResults.Count == 0
                    ? "No cards matched your search."
                    : $"{SearchResults.Count} card(s) found.";
            }
            catch (HttpRequestException exception)
            {
                StatusMessage = $"Card search failed: {exception.Message}";
            }
        }

        /// <summary>Adds the selected search result to the selected group.</summary>
        public void AddSelectedCard()
        {
            if (SelectedGroup is null || SelectedCard is null)
            {
                StatusMessage = "Select a group and a card first.";
                return;
            }

            if (SelectedGroup.CardIds.Contains(SelectedCard.Id))
            {
                StatusMessage = $"{SelectedCard.Name} is already in this group.";
                return;
            }

            SelectedGroup.CardIds.Add(SelectedCard.Id);
            SelectedGroup.RepresentativeCardId ??= SelectedCard.Id;
            GroupCards.Add(SelectedCard);
            SelectedGroupCard = SelectedCard;
            StatusMessage = $"{SelectedCard.Name} added to {SelectedGroup.Name}.";
        }

        /// <summary>Removes the selected group card.</summary>
        public void RemoveSelectedCard()
        {
            if (SelectedGroup is null || SelectedGroupCard is null)
            {
                StatusMessage = "Select a card in the group first.";
                return;
            }

            if (SelectedGroup.CardIds.Remove(SelectedGroupCard.Id))
            {
                string cardName = SelectedGroupCard.Name;
                GroupCards.Remove(SelectedGroupCard);
                if (SelectedGroup.RepresentativeCardId == SelectedGroupCard.Id)
                {
                    SelectedGroup.RepresentativeCardId = SelectedGroup.CardIds.FirstOrDefault();
                    if (SelectedGroup.CardIds.Count == 0)
                    {
                        SelectedGroup.RepresentativeCardId = null;
                    }
                }
                SelectedGroupCard = null;
                StatusMessage = $"{cardName} removed from {SelectedGroup.Name}.";
            }
        }

        /// <summary>Loads a group library into the editor.</summary>
        public void Load(BotGroupLibrary library)
        {
            Groups.Clear();
            foreach (BotCardGroup group in library.Groups)
            {
                Groups.Add(Clone(group));
            }
            SelectedGroup = Groups.FirstOrDefault();
            StatusMessage = "Group library loaded.";
        }

        /// <summary>Saves the current group library.</summary>
        public async Task SaveAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var library = new BotGroupLibrary { Groups = Groups.Select(Clone).ToList() };
            await _libraryService.WriteFileAsync(library, filePath, cancellationToken);
            FilePath = filePath;
            StatusMessage = $"Group library saved to {filePath}.";
        }

        /// <summary>Loads a group library from disk.</summary>
        public async Task LoadAsync(string filePath, CancellationToken cancellationToken = default)
        {
            Load(await _libraryService.ReadFileAsync(filePath, cancellationToken));
            FilePath = filePath;
        }

        /// <summary>Shows a file operation error in the editor.</summary>
        public void ReportError(string message) => StatusMessage = $"Group library operation failed: {message}";

        private async Task RefreshGroupCardsAsync()
        {
            GroupCards.Clear();
            if (SelectedGroup is null)
            {
                return;
            }

            try
            {
                foreach (int id in SelectedGroup.CardIds)
                {
                    CardModel? card = SearchResults.FirstOrDefault(result => result.Id == id)
                        ?? await _cardInfoService.GetCardByIdAsync(id);
                    if (card is not null && GroupCards.All(existing => existing.Id != card.Id))
                    {
                        GroupCards.Add(card);
                    }
                }
            }
            catch (HttpRequestException exception)
            {
                StatusMessage = $"Group cards could not be loaded: {exception.Message}";
            }
            catch (IOException exception)
            {
                StatusMessage = $"Group card cache could not be accessed: {exception.Message}";
            }
        }

        private static BotCardGroup Clone(BotCardGroup group) => new()
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            RepresentativeCardId = group.RepresentativeCardId,
            CardIds = group.CardIds.ToList()
        };
    }
}
