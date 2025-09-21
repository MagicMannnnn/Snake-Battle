using System;
using System.Collections.Generic;
using System.Linq;


public class Point
{
    public int x { get; set; }
    public int y { get; set; }
}

public class GameState
{
    public List<Point> snake { get; set; } = new();
    public Point apple { get; set; } = new();
    public int score { get; set; }
    public bool gameOver { get; set; }
}



public class Test
{
    private AI ai;
    private SnakeGame game;
    private int gridSize;
    private bool CNN = true;
    private int visionSize;
    private string path;
    private bool alive = true;

    public Test(string path, int visionSize = 7, int gridSize = 20)
    {
        this.visionSize = visionSize;
        this.gridSize = gridSize;
        game = new SnakeGame(gridSize);
        ai = new AI();
        this.path = path;
        try
        {
            ai.Load(path);
        }catch
        {

        }
        
    }

    public void Load()
    {
        ai.Load(path);
    }

    public void Reset()
    {
        alive = true;
        game.Reset();
        Load();
    }

    public void Update()
    {

        if (!alive)
        {
            return;
        }

        List<double> state = GetStateVector();

        var qValues = ai.GetQValues(state);
        int action = qValues.IndexOf(qValues.Max());
        game.SetDirection(action);

        alive = game.Move();

        
    }

    private List<double> GetStateVector()
    {
        if (CNN)
        {
            return GetStateVectorCNN(); // returns grid + apple direction
        }

        var state = new List<double>();
        var head = game.Snake[0];
        int headX = head.x;
        int headY = head.y;

        var snakeSet = new HashSet<(int, int)>(game.Snake);
        int half = visionSize / 2;

        for (int dx = -half; dx <= half; dx++)
        {
            for (int dy = -half; dy <= half; dy++)
            {
                int nx = headX + dx;
                int ny = headY + dy;
                if (dx == 0 && dy == 0) continue;

                if (nx < 0 || ny < 0 || nx >= gridSize || ny >= gridSize)
                    state.Add(-1);
                else if (snakeSet.Contains((nx, ny)))
                    state.Add(-0.5);
                else if (nx == game.Apple.x && ny == game.Apple.y)
                    state.Add(1);
                else
                    state.Add(0);
            }
        }

        // Add apple direction
        int deltaX = game.Apple.x - headX;
        int deltaY = game.Apple.y - headY;
        state.Add((double)Math.Sign(deltaX));
        state.Add((double)Math.Sign(deltaY));

        return state;
    }

    private double[,] GetStateGrid()
    {
        int headX = game.Snake[0].x;
        int headY = game.Snake[0].y;

        double[,] grid = new double[visionSize, visionSize];
        var snakeSet = new HashSet<(int, int)>(game.Snake);
        int half = visionSize / 2;

        int appleX = game.Apple.x;
        int appleY = game.Apple.y;

        for (int dx = -half; dx <= half; dx++)
        {
            for (int dy = -half; dy <= half; dy++)
            {
                int nx = headX + dx;
                int ny = headY + dy;
                int i = dx + half;
                int j = dy + half;

                if (nx < 0 || ny < 0 || nx >= gridSize || ny >= gridSize)
                    grid[i, j] = -1; // wall
                else if (snakeSet.Contains((nx, ny)))
                    grid[i, j] = -0.5; // snake body
                else
                    grid[i, j] = 0; // empty
            }
        }

        // --- Encode apple direction inside grid ---
        int dxDir = Math.Sign(appleX - headX);
        int dyDir = Math.Sign(appleY - headY);

        int appleGridX = half + dxDir * half; // left/right edge
        int appleGridY = half + dyDir * half; // top/bottom edge

        // Mark apple roughly in that direction
        if (appleGridX >= 0 && appleGridX < visionSize && appleGridY >= 0 && appleGridY < visionSize)
            grid[appleGridX, appleGridY] = 1;

        return grid;
    }


    private List<double> GetStateVectorCNN()
    {
        var grid = GetStateGrid();
        List<double> flat = new List<double>();
        for (int i = 0; i < visionSize; i++)
            for (int j = 0; j < visionSize; j++)
                flat.Add(grid[i, j]);

        return flat;
    }

    public int GetScore() => game.GetScore();

    public GameState getState()
    {
        return new GameState
        {
            snake = game.Snake.Select(s => new Point { x = s.x, y = s.y }).ToList(),
            apple = new Point { x = game.Apple.x, y = game.Apple.y },
            score = game.GetScore(),
            gameOver = game.GameOver
        };
    }

}
