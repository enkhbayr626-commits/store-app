using store_app.Services;

namespace store_app;

public partial class GamePage : ContentPage
{
    const double BirdWidth = 46;
    const double BirdHeight = 34;
    const double PipeWidth = 68;
    const double GroundHeight = 74;
    const double Gravity = 0.45;
    const double FlapVelocity = -8.2;

    readonly Random random = new();
    readonly List<PipePair> pipes = [];

    IDispatcherTimer? gameTimer;
    double gameWidth;
    double gameHeight;
    double birdX;
    double birdY;
    double birdVelocity;
    double landX1;
    double landX2;
    int score;
    int frame;
    bool isRunning;
    bool isGameOver;
    bool layoutReady;

    public GamePage()
    {
        InitializeComponent();

        pipes.Add(new PipePair(PipeTop1, PipeBottom1));
        pipes.Add(new PipePair(PipeTop2, PipeBottom2));
        pipes.Add(new PipePair(PipeTop3, PipeBottom3));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameLoyaltyLabel.Text = $"{AppState.LoyaltyPoints:#,0}";

        gameTimer ??= Dispatcher.CreateTimer();
        gameTimer.Interval = TimeSpan.FromMilliseconds(16);
        gameTimer.Tick -= OnGameTick;
        gameTimer.Tick += OnGameTick;
        gameTimer.Start();

        if (layoutReady)
        {
            ResetGame();
        }
    }

    protected override void OnDisappearing()
    {
        gameTimer?.Stop();
        base.OnDisappearing();
    }

    async void OnBackClicked(object? sender, EventArgs e)
    {
        gameTimer?.Stop();

        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Shell.Current.GoToAsync("//app/loyalty");
    }

    void OnGameAreaSizeChanged(object? sender, EventArgs e)
    {
        if (GameArea.Width <= 0 || GameArea.Height <= 0)
        {
            return;
        }

        gameWidth = GameArea.Width;
        gameHeight = GameArea.Height;
        layoutReady = true;
        ResetGame();
    }

    void OnPlaySurfaceTapped(object? sender, TappedEventArgs e)
    {
        if (!layoutReady)
        {
            return;
        }

        if (isGameOver)
        {
            ResetGame();
        }

        if (!isRunning)
        {
            isRunning = true;
            HintLabel.IsVisible = false;
        }

        birdVelocity = FlapVelocity;
    }

    void OnRestartClicked(object? sender, EventArgs e)
    {
        ResetGame();
    }

    void OnGameTick(object? sender, EventArgs e)
    {
        if (!layoutReady)
        {
            return;
        }

        frame++;
        MoveGround();

        if (isRunning && !isGameOver)
        {
            birdVelocity += Gravity;
            birdY += birdVelocity;

            foreach (var pipe in pipes)
            {
                pipe.X -= PipeSpeed;

                if (pipe.X < -PipeWidth)
                {
                    ResetPipe(pipe, FindRightmostPipeX() + PipeSpacing);
                }

                if (!pipe.Scored && pipe.X + PipeWidth < birdX)
                {
                    pipe.Scored = true;
                    score++;
                    GameSound.PlayPoint();
                    ScoreLabel.Text = $"{score}";
                }
            }

            if (HasCollision())
            {
                EndGame();
            }
        }
        else if (!isGameOver)
        {
            birdY += Math.Sin(frame / 12.0) * 0.28;
        }

        UpdateScene();
    }

    void ResetGame()
    {
        isRunning = false;
        isGameOver = false;
        score = 0;
        frame = 0;
        birdX = Math.Max(74, gameWidth * 0.23);
        birdY = Math.Max(120, gameHeight * 0.38);
        birdVelocity = 0;
        landX1 = 0;
        landX2 = gameWidth;

        ScoreLabel.Text = "0";
        GameLoyaltyLabel.Text = $"{AppState.LoyaltyPoints:#,0}";
        HintLabel.IsVisible = true;
        HintLabel.Text = "Товшоод нисгэнэ";
        GameOverPanel.IsVisible = false;

        for (var i = 0; i < pipes.Count; i++)
        {
            ResetPipe(pipes[i], gameWidth + 130 + (i * PipeSpacing));
        }

        UpdateScene();
    }

    void EndGame()
    {
        if (isGameOver)
        {
            return;
        }

        isRunning = false;
        isGameOver = true;

        AppState.AddGameReward(score, "Flappy Bird");
        GameLoyaltyLabel.Text = $"{AppState.LoyaltyPoints:#,0}";
        FinalScoreLabel.Text = $"Оноо: {score}";
        EarnedPointsLabel.Text = $"+{score:#,0} loyalty оноо нэмэгдлээ";
        HintLabel.IsVisible = false;
        GameOverPanel.IsVisible = true;
    }

    void MoveGround()
    {
        var speed = isGameOver ? 0 : PipeSpeed;
        landX1 -= speed;
        landX2 -= speed;

        if (landX1 <= -gameWidth)
        {
            landX1 = landX2 + gameWidth;
        }

        if (landX2 <= -gameWidth)
        {
            landX2 = landX1 + gameWidth;
        }
    }

    void UpdateScene()
    {
        var groundY = GroundY;
        AbsoluteLayout.SetLayoutBounds(BirdImage, new Rect(birdX, birdY, BirdWidth, BirdHeight));
        BirdImage.Rotation = Math.Clamp(birdVelocity * 4.2, -28, 70);
        BirdImage.Source = (frame % 18) switch
        {
            < 6 => "flappy_bird_1.png",
            < 12 => "flappy_bird_2.png",
            _ => "flappy_bird_3.png",
        };

        foreach (var pipe in pipes)
        {
            var topHeight = Math.Max(30, pipe.GapY - PipeGap / 2);
            var bottomY = pipe.GapY + PipeGap / 2;
            var bottomHeight = Math.Max(30, groundY - bottomY);

            AbsoluteLayout.SetLayoutBounds(pipe.Top, new Rect(pipe.X, 0, PipeWidth, topHeight));
            AbsoluteLayout.SetLayoutBounds(pipe.Bottom, new Rect(pipe.X, bottomY, PipeWidth, bottomHeight));
        }

        AbsoluteLayout.SetLayoutBounds(Land1, new Rect(landX1, groundY, gameWidth, GroundHeight));
        AbsoluteLayout.SetLayoutBounds(Land2, new Rect(landX2, groundY, gameWidth, GroundHeight));
    }

    void ResetPipe(PipePair pipe, double x)
    {
        pipe.X = x;
        pipe.GapY = RandomGapY();
        pipe.Scored = false;
    }

    double RandomGapY()
    {
        var minCenter = 96 + PipeGap / 2;
        var maxCenter = GroundY - 42 - PipeGap / 2;

        if (maxCenter <= minCenter)
        {
            return gameHeight * 0.45;
        }

        return minCenter + random.NextDouble() * (maxCenter - minCenter);
    }

    double FindRightmostPipeX()
    {
        return pipes.Max(pipe => pipe.X);
    }

    bool HasCollision()
    {
        var groundY = GroundY;

        if (birdY < 0 || birdY + BirdHeight > groundY)
        {
            return true;
        }

        var birdRect = new Rect(birdX + 8, birdY + 6, BirdWidth - 16, BirdHeight - 12);

        foreach (var pipe in pipes)
        {
            var topHeight = Math.Max(30, pipe.GapY - PipeGap / 2);
            var bottomY = pipe.GapY + PipeGap / 2;
            var topRect = new Rect(pipe.X + 8, 0, PipeWidth - 16, topHeight);
            var bottomRect = new Rect(pipe.X + 8, bottomY, PipeWidth - 16, groundY - bottomY);

            if (Intersects(birdRect, topRect) || Intersects(birdRect, bottomRect))
            {
                return true;
            }
        }

        return false;
    }

    double GroundY => Math.Max(0, gameHeight - GroundHeight);

    double PipeGap => Math.Clamp(gameHeight * 0.28, 138, 170);

    double PipeSpacing => Math.Clamp(gameWidth * 0.58, 198, 245);

    double PipeSpeed => Math.Clamp(gameWidth / 132, 2.4, 3.4);

    static bool Intersects(Rect first, Rect second)
    {
        return first.Left < second.Right
            && first.Right > second.Left
            && first.Top < second.Bottom
            && first.Bottom > second.Top;
    }

    sealed class PipePair(Image top, Image bottom)
    {
        public Image Top { get; } = top;

        public Image Bottom { get; } = bottom;

        public double X { get; set; }

        public double GapY { get; set; }

        public bool Scored { get; set; }
    }
}
