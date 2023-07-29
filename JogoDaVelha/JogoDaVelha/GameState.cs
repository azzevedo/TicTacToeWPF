using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JogoDaVelha;

internal class GameState
{
	#nullable disable
	public Player[,] GameGrid { get; private set; }
	public Player CurrentPlayer { get; private set; }
    public int TurnsPassed { get; private set; } = 0;
	public bool GameOver { get; private set; } = false;


	public event Action<int, int> MoveMade;
	public event Action<GameResult> GameEnded;
	public event Action GameRestarted;


	public GameState(Player currentPlayer = Player.X)
	{
		GameGrid = new Player[3, 3];
		CurrentPlayer = currentPlayer;
	}


	bool CanMarkMove(int r, int c) => !GameOver && GameGrid[r, c] == Player.None;
	bool IsGridFull() => TurnsPassed == 9;
	void SwitchPlayer() => CurrentPlayer = CurrentPlayer == Player.X ? Player.O : Player.X;

	bool AreSquaresMarked((int, int)[] squares, Player player)
	{
		foreach ((int r, int c) in squares)
		{
			if (GameGrid[r, c] != player) return false;
		}

		return true;
	}

	bool DidMoveWin(int r, int c, out WinInfo winInfo)
	{
		(int, int)[] row = new[] { (r, 0), (r, 1), (r, 2) };
		(int, int)[] col = new[] { (0, c), (1, c), (2, c) };
		(int, int)[] mainDiag = new[] { (0, 0), (1, 1), (2, 2) };
		(int, int)[] antiDiag = new[] { (0, 2), (1, 1), (2, 0) };

		winInfo = new WinInfo();

		if (AreSquaresMarked(row, CurrentPlayer))
		{
			winInfo.Type = WinType.Row;
			winInfo.Number = r;
			return true;
		}

		if (AreSquaresMarked(col, CurrentPlayer))
		{
			winInfo.Type = WinType.Column;
			winInfo.Number = c;
			return true;
		}

		if (AreSquaresMarked(mainDiag, CurrentPlayer))
		{
			winInfo.Type = WinType.MainDiagonal;
			return true;
		}

		if (AreSquaresMarked(antiDiag, CurrentPlayer))
		{
			winInfo.Type = WinType.AntiDiagonal;
			return true;
		}

		winInfo = null;
		return false;
	}

	bool DidMoveEndGame(int r, int c, out GameResult gameResult)
	{
		if (DidMoveWin(r, c, out WinInfo winInfo))
		{
			gameResult = new() { Winner = CurrentPlayer, WinInfo = winInfo };
			return true;
		}

		if (IsGridFull())
		{
			gameResult = new() { Winner = Player.None };
			return true;
		}

		gameResult = null;
		return false;
	}

	public void MakeMove(int r, int c)
	{
		if (!CanMarkMove(r, c)) return;

		GameGrid[r, c] = CurrentPlayer;
		TurnsPassed++;

		if (DidMoveEndGame(r, c, out GameResult gameResult))
		{
			GameOver = true;
			MoveMade?.Invoke(r, c);
			GameEnded?.Invoke(gameResult);
		}
		else
		{
			SwitchPlayer();
			MoveMade?.Invoke(r, c);
		}
	}

	public void Reset(Player currentPlayer = Player.X)
	{
		GameGrid = new Player[3,3];
		CurrentPlayer = currentPlayer;
		TurnsPassed = 0;
		GameOver = false;
		GameRestarted?.Invoke();
	}
}
