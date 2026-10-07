using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using store_app.Services;

namespace store_app;

public partial class MemoryGamePage : ContentPage
{
    const int PairCount = 8;
    const int PointsPerMatch = 2;
    const int CompletionBonus = 10;

    readonly Random random = new();

    MemoryCard? firstCard;
    MemoryCard? secondCard;
    bool isChecking;
    int moves;
    int matches;
    int earned;

    public MemoryGamePage()
    {
        InitializeComponent();
        BindingContext = this;
        ResetGame();
    }

    public ObservableCollection<MemoryCard> Cards { get; } = [];

    async void OnBackClicked(object? sender, EventArgs e)
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Shell.Current.GoToAsync("//app/loyalty");
    }

    void OnRestartClicked(object? sender, EventArgs e)
    {
        ResetGame();
    }

    async void OnCardTapped(object? sender, TappedEventArgs e)
    {
        if (isChecking || (sender as Element)?.BindingContext is not MemoryCard card)
        {
            return;
        }

        if (card.IsMatched || card.IsRevealed)
        {
            return;
        }

        card.IsRevealed = true;

        if (firstCard is null)
        {
            firstCard = card;
            return;
        }

        secondCard = card;
        moves++;
        RefreshStats();

        if (firstCard.PairId == secondCard.PairId)
        {
            firstCard.IsMatched = true;
            secondCard.IsMatched = true;
            matches++;
            AddGamePoints(PointsPerMatch);
            ClearSelection();

            if (matches == PairCount)
            {
                AddGamePoints(CompletionBonus);
                AppState.AddGameHistory(earned, "Memory Card", "бүх хосыг олсон");
                await Navigation.PushModalAsync(
                    new AppNoticePage("Баяр хүргэе", "Memory card тоглоом амжилттай дууслаа.", $"+{earned} loyalty оноо", "★"),
                    false);
            }

            RefreshStats();
            return;
        }

        isChecking = true;
        await Task.Delay(650);
        firstCard.IsRevealed = false;
        secondCard.IsRevealed = false;
        ClearSelection();
        isChecking = false;
    }

    void ResetGame()
    {
        firstCard = null;
        secondCard = null;
        isChecking = false;
        moves = 0;
        matches = 0;
        earned = 0;

        var ids = Enumerable.Range(1, PairCount)
            .Concat(Enumerable.Range(1, PairCount))
            .OrderBy(_ => random.Next())
            .ToList();

        Cards.Clear();
        for (var index = 0; index < ids.Count; index++)
        {
            Cards.Add(new MemoryCard(index, ids[index], $"memory_{ids[index]}.png"));
        }

        RefreshStats();
    }

    void AddGamePoints(int points)
    {
        earned += points;
        AppState.AddLoyaltyPoints(points);
    }

    void ClearSelection()
    {
        firstCard = null;
        secondCard = null;
    }

    void RefreshStats()
    {
        MovesLabel.Text = $"{moves}";
        MatchesLabel.Text = $"{matches}/{PairCount}";
        EarnedLabel.Text = $"+{earned}";
    }
}

public sealed class MemoryCard : INotifyPropertyChanged
{
    bool isRevealed;
    bool isMatched;

    public MemoryCard(int id, int pairId, string frontImage)
    {
        Id = id;
        PairId = pairId;
        FrontImage = frontImage;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; }

    public int PairId { get; }

    public string FrontImage { get; }

    public bool IsRevealed
    {
        get => isRevealed;
        set
        {
            if (isRevealed == value)
            {
                return;
            }

            isRevealed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayImage));
        }
    }

    public bool IsMatched
    {
        get => isMatched;
        set
        {
            if (isMatched == value)
            {
                return;
            }

            isMatched = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayImage));
        }
    }

    public string DisplayImage => IsRevealed || IsMatched
        ? FrontImage
        : "memory_card_back.svg";

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
