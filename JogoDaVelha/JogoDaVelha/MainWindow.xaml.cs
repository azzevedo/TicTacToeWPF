using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace JogoDaVelha;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	readonly IReadOnlyDictionary<Player, ImageSource> imageSources = new Dictionary<Player, ImageSource>()
	{
		{ Player.X, new BitmapImage(new Uri("pack://application:,,,/Assets/X15.png")) },
		{ Player.O, new BitmapImage(new Uri("pack://application:,,,/Assets/O15.png")) },
	};

	/// <summary>
	/// Modo de fazer uma animação no WPF, com os sprites desta sequência de imagens
	/// </summary>
	readonly Dictionary<Player, ObjectAnimationUsingKeyFrames> animations = new()
	{
		{ Player.X, new ObjectAnimationUsingKeyFrames() },
		{ Player.O, new ObjectAnimationUsingKeyFrames() },
	};

	readonly DoubleAnimation fadeOutAnimation = new()
	{
		Duration = TimeSpan.FromSeconds(.4),
		From = 1,
		To = 0,
	};
	readonly DoubleAnimation fadeInAnimation = new()
	{
		Duration = TimeSpan.FromSeconds(.4),
		From = 0,
		To = 1,
	};

	readonly Image[,] imageControls = new Image[3, 3];
	readonly GameState gameState = new();


	public MainWindow()
	{
		InitializeComponent();
		SetupGameGrid();
		SetupAnimations();

		gameState.MoveMade += OnMoveMade;
		gameState.GameEnded += OnGameEnded;
		gameState.GameRestarted += OnGameRestarted;
	}

	void SetupGameGrid()
	{
		for (int r = 0; r < 3; r++)
			for (int c = 0; c < 3; c++)
			{
				Image imageControl = new();
				GameGrid.Children.Add(imageControl);
				imageControls[r, c] = imageControl;
			}
	}

	void SetupAnimations()
	{
		animations[Player.X].Duration = TimeSpan.FromSeconds(.25);
		animations[Player.O].Duration = TimeSpan.FromSeconds(.25);

		for (int i = 0; i < 16; i++)
		{
			Uri xUri = new($"pack://application:,,,/Assets/X{i}.png");
			BitmapImage xImg = new(xUri);
			DiscreteObjectKeyFrame xKeyFrame = new(xImg);
			animations[Player.X].KeyFrames.Add(xKeyFrame);

			Uri oUri = new($"pack://application:,,,/Assets/O{i}.png");
			BitmapImage oImg = new(oUri);
			DiscreteObjectKeyFrame oKeyFrame = new(oImg);
			animations[Player.O].KeyFrames.Add(oKeyFrame);
		}
	}

	void OnMoveMade(int r, int c)
	{
		Player player = gameState.GameGrid[r, c];
		//imageControls[r, c].Source = imageSources[player];
		imageControls[r, c].BeginAnimation(Image.SourceProperty, animations[player]);
		PlayerImage.Source = imageSources[gameState.CurrentPlayer];
	}

	(Point, Point) FindLinePoints(WinInfo winInfo)
	{
		double squareSize = GameGrid.Width / 3;
		double margin = squareSize / 2;

		double y = winInfo.Number * squareSize + margin;
		double x = winInfo.Number * squareSize + margin;

		if (winInfo.Type == WinType.Row)
			return (new Point(0, y), new Point(GameGrid.Width, y));

		if (winInfo.Type == WinType.Column)
			return (new Point(x, 0), new Point(x, GameGrid.Height));

		if (winInfo.Type == WinType.MainDiagonal)
			return (new Point(0, 0),  new Point(GameGrid.Width, GameGrid.Height));

		//WinType.AntiDiagonal
		return (new Point(GameGrid.Width, 0), new Point(0, GameGrid.Height));
	}

	async Task ShowLine(WinInfo winInfo)
	{
		(Point start, Point end) = FindLinePoints(winInfo);
		Line.X1 = start.X;
		Line.Y1 = start.Y;

		//Line.X2 = end.X;
		//Line.Y2	= end.Y;
		DoubleAnimation x2 = new()
		{
			Duration = TimeSpan.FromSeconds(.25),
			From = start.X,
			To = end.X,
		};
		DoubleAnimation y2 = new()
		{
			Duration = TimeSpan.FromSeconds(.25),
			From = start.Y,
			To = end.Y,
		};

		Line.Visibility = Visibility.Visible;
		Line.BeginAnimation(Line.X2Property, x2);
		Line.BeginAnimation(Line.Y2Property, y2);

		await Task.Delay(x2.Duration.TimeSpan);
	}

	async Task FadeOut(UIElement uiElement)
	{
		uiElement.BeginAnimation(OpacityProperty, fadeOutAnimation);
		await Task.Delay(fadeOutAnimation.Duration.TimeSpan);
		uiElement.Visibility = Visibility.Hidden;
	}
	async Task FadeIn(UIElement uiElement)
	{
		uiElement.BeginAnimation(OpacityProperty, fadeInAnimation);
		await Task.Delay(fadeInAnimation.Duration.TimeSpan);
		uiElement.Visibility = Visibility.Visible;
	}

	async Task TransitionToGameScreen()
	{
		//EndScreen.Visibility = Visibility.Hidden;
		await Task.WhenAll(FadeOut(EndScreen));
		Line.Visibility = Visibility.Hidden;

		//TurnPanel.Visibility = Visibility.Visible;
		//GameCanvas.Visibility = Visibility.Visible;
		await Task.WhenAll(FadeIn(TurnPanel), FadeIn(GameCanvas));
	}

	async Task TransitionToEndScreen(string text, ImageSource? winnerImage)
	{
		//TurnPanel.Visibility = Visibility.Hidden;
		//GameCanvas.Visibility = Visibility.Hidden;
		await Task.WhenAll(FadeOut(TurnPanel), FadeOut(GameCanvas));

		ResultText.Text = text;
		WinnerImage.Source = winnerImage;

		//EndScreen.Visibility = Visibility.Visible;
		await FadeIn(EndScreen);
	}

	async void OnGameEnded(GameResult gameResult)
	{
		await Task.Delay(1000);

		if (gameResult.Winner == Player.None)
			await TransitionToEndScreen("Empate!", null);
		else
		{
			await ShowLine(gameResult.WinInfo!);
			await Task.Delay(1000);
			await TransitionToEndScreen("Vencedor:", imageSources[gameResult.Winner]);
		}

	}

	async void OnGameRestarted()
	{
		for (int r = 0; r < 3; r++)
			for (int c = 0; c < 3; c++)
			{
				// sem isso, as imagens (animações) não resetam
				imageControls[r, c].BeginAnimation(Image.SourceProperty, null);
				imageControls[r, c].Source = null;
			}

		PlayerImage.Source = imageSources[gameState.CurrentPlayer];
		await TransitionToGameScreen();
	}

	private void GameGrid_MouseDown(object sender, MouseButtonEventArgs e)
	{
		double squareSize = GameGrid.Width / 3;
		Point clickPosition = e.GetPosition(GameGrid);
		int row = (int)(clickPosition.Y / squareSize);
		int col = (int)(clickPosition.X / squareSize);

		gameState.MakeMove(row, col);
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		if (gameState.GameOver)
			gameState.Reset();
	}
}
