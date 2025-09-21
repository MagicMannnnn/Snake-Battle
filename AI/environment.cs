using System;
using System.Collections.Generic;

public class SnakeEnvironment
{
    private SnakeGame game;
    private int gridSize;
    private bool CNN = true;
    private int visionSize;

    public SnakeEnvironment(int visionSize = 7, int gridSize = 20)
    {
        this.visionSize = visionSize;
        this.gridSize = gridSize;
        game = new SnakeGame(gridSize);
    }

    public List<double> Reset()
    {
        game.Reset();
        return GetStateVector();
    }

    public (List<double> nextState, double reward, bool done) Step(int action)
    {
        int prevScore = game.GetScore();
        var prevHead = game.Snake[0];

        game.SetDirection(action);
        bool alive = game.Move();

        double reward = -0.01; // step penalty

        if (!alive)
        {
            reward = -1.0;
        }
        else if (game.GetScore() != prevScore)
        {
            reward = +1.0;
        }
        else
        {
            int prevDistance = Math.Abs(prevHead.x - game.Apple.x) + Math.Abs(prevHead.y - game.Apple.y);
            int newDistance = Math.Abs(game.Snake[0].x - game.Apple.x) + Math.Abs(game.Snake[0].y - game.Apple.y);

            if (newDistance < prevDistance)
                reward += 0.01;
        }

        return (GetStateVector(), reward, !alive);
    }

    public List<double> GetStateVector()
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
}
