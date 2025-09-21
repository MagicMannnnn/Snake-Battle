using System;
using System.Collections.Generic;

public class SnakeGame
{
    public int GridSize { get; private set; } = 20;
    public List<(int x, int y)> Snake { get; private set; }
    public (int x, int y) Apple { get; private set; }
    public bool GameOver { get; private set; } = false;
    private Random rnd = new Random();

    private int direction = 0; // 0=up, 1=right, 2=down, 3=left

    public SnakeGame(int gridSize = 20)
    {
        GridSize = gridSize;
        Reset();
    }

    public void Reset()
    {
        Snake = new List<(int x, int y)>();
        int startX = GridSize / 2;
        int startY = GridSize / 2;

        // Start length of 3, going up
        for (int i = 0; i < 3; i++)
        {
            Snake.Add((startX, startY + i)); // head is first element
        }

        direction = 0; // up
        PlaceApple();
        GameOver = false;
    }

    public void SetDirection(int newDirection)
    {
        // Prevent reversing direction
        if ((direction + 2) % 4 != newDirection)
            direction = newDirection;
    }

    public void PlaceApple()
    {
        int x, y;
        do
        {
            x = rnd.Next(0, GridSize);
            y = rnd.Next(0, GridSize);
        } while (Snake.Contains((x, y)));
        Apple = (x, y);
    }

    public bool Move()
    {
        if (GameOver) return false;

        var head = Snake[0];
        (int x, int y) newHead = head;

        switch (direction)
        {
            case 0: newHead = (head.x, head.y - 1); break; // up
            case 1: newHead = (head.x + 1, head.y); break; // right
            case 2: newHead = (head.x, head.y + 1); break; // down
            case 3: newHead = (head.x - 1, head.y); break; // left
        }

        // Collision check
        if (newHead.x < 0 || newHead.y < 0 || newHead.x >= GridSize || newHead.y >= GridSize || Snake.Contains(newHead))
        {
            GameOver = true;
            return false;
        }

        Snake.Insert(0, newHead);

        // Eat apple
        if (newHead == Apple)
        {
            PlaceApple();
        }
        else
        {
            Snake.RemoveAt(Snake.Count - 1);
        }

        return true;
    }

    public int GetScore()
    {
        return Snake.Count - 3; // starting length is 3
    }

    public static SnakeGame FromMessage(string msg, int gridSize = 20)
    {
        var game = new SnakeGame(gridSize);

        // Use List<List<int>> for snake to match old format
        List<List<int>> snake = new List<List<int>>();
        (int x, int y) apple = (0, 0);

        try
        {
            int appleIndex = msg.IndexOf("apple:");
            int snakeIndex = msg.IndexOf("snake:");

            if (appleIndex >= 0 && snakeIndex > appleIndex)
            {
                // --- Apple ---
                string applePart = msg.Substring(appleIndex + 6, snakeIndex - (appleIndex + 6)).Trim();
                var appleCoords = applePart.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (appleCoords.Length >= 2)
                {
                    apple = (int.Parse(appleCoords[0].Trim()), int.Parse(appleCoords[1].Trim()));
                }

                // --- Snake ---
                string snakePart = msg.Substring(snakeIndex + 6).Trim();
                int dashIndex = snakePart.IndexOf('-');
                if (dashIndex >= 0)
                {
                    string segmentsPart = snakePart.Substring(dashIndex + 1).Trim();
                    var segmentStrings = segmentsPart.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var seg in segmentStrings)
                    {
                        var coords = seg.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (coords.Length == 2)
                        {
                            snake.Add(new List<int>
                        {
                            int.Parse(coords[0].Trim()),
                            int.Parse(coords[1].Trim())
                        });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error parsing SnakeGame message: " + ex.Message);
        }

        // Populate SnakeGame with parsed values
        game.Snake.Clear();
        foreach (var seg in snake)
        {
            game.Snake.Add((seg[0], seg[1]));
        }
        game.Apple = apple;

        return game;
    }

}
