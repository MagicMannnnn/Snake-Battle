using System;
using System.Collections.Generic;
using System.Linq;

public class AI
{
    private Network network;
    private int gridSize;
    private int score = 0;
    private int visionSize;

    public AI(int visionSize = 7, int gridSize = 20, int inputSize = 10)
    {
        this.visionSize = visionSize;
        this.gridSize = gridSize;

        
        network = new Network();
        network.AddLayerC("Conv2D", visionSize, visionSize, 3, 3, 8, 1);
        network.AddLayer("Relu");

        int outH = (visionSize - 3) / 1 + 1; // 5
        int outW = (visionSize - 3) / 1 + 1; // 5
        int flattenedSize = outH * outW * 8; // 200

        network.AddLayer("Dense", flattenedSize, 64);
        network.AddLayer("Relu");
        network.AddLayer("Dense", 64, 4); // linear

    }

    #region Q-Learning Methods
    /// <summary>
    /// Returns Q-values for the given state.
    /// </summary>
    public List<double> GetQValues(List<double> state)
    {
        return network.ForwardPass(state);
    }

    /// <summary>
    /// Performs one gradient step on (state, targetQValues).
    /// </summary>
    public void TrainStep(List<List<double>> states, List<List<double>> targetQValues, int episodes = 15, float learningRate = 0.005f)
    {
        if (states == null || targetQValues == null || states.Count == 0 || targetQValues.Count == 0)
            throw new ArgumentException("States and targetQValues cannot be null or empty.");

        if (states.Count != targetQValues.Count)
            throw new ArgumentException("States and targetQValues must have the same length.");

        // Directly train the network on the batch
        network.Train(episodes, learningRate, states, targetQValues);
    }
    #endregion

    #region Helpers for SnakeGame
    /// <summary>
    /// Convert SnakeGame state into input features for the network.
    /// </summary>
    public List<double> EncodeState(SnakeGame game)
    {
        var snake = game.Snake;
        var apple = game.Apple;

        if (snake.Count == 0)
            return Enumerable.Repeat(0.0, 10).ToList();

        int headX = snake[0].x;
        int headY = snake[0].y;

        List<double> input = new List<double>();

        // 8 neighbors around the head
        int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

        HashSet<(int, int)> snakeSet = new HashSet<(int, int)>(snake);

        for (int i = 0; i < 8; i++)
        {
            int nx = headX + dx[i];
            int ny = headY + dy[i];

            if (nx < 0 || nx >= gridSize || ny < 0 || ny >= gridSize)
                input.Add(-1); // wall
            else if (snakeSet.Contains((nx, ny)))
                input.Add(-0.5); // snake body
            else if (nx == apple.x && ny == apple.y)
                input.Add(1); // apple
            else
                input.Add(0); // empty
        }

        // Direction to apple
        int deltaX = apple.x - headX;
        int deltaY = apple.y - headY;

        input.Add(Math.Sign(deltaX));
        input.Add(Math.Sign(deltaY));

        return input;
    }
    #endregion

    #region Persistence
    public void Save(string path) => network.Save(path);
    public void Load(string path) => network.Load(path);
    public void LoadFromNetwork(AI other) => network.LoadFrom(other.network);
    #endregion

    #region Score Tracking
    public int GetScore() => score;
    public void SetScore(int value) => score = value;
    #endregion
}
