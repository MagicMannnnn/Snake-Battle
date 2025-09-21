import React from "react";
import "./App.css";

export default function SnakeBoard({
  snake,
  food,
  rows,
  cols,
  cellRoundness,
  title,
  score,
  highScore,
  onRestart,
}) {
  const boardStyle = {
    "--rows": `${rows}`,
    "--cols": `${cols}`,
    aspectRatio: "1 / 1", // keep the board square
    width: "100%",
    height: "100%",
  };

  const cells = Array.from({ length: rows * cols });
  const cellElements = cells.map((_, i) => {
    const snakeIndex = snake.findIndex(([x, y]) => y * cols + x === i);
    const foodIndex = food[1] * cols + food[0] === i ? true : false;

    const color =
      snakeIndex !== -1
        ? `hsl(${snakeIndex * 15 + 95}, 85%, 50%)`
        : foodIndex
        ? "red"
        : null;

    return (
      <div
        key={i}
        className="cell"
        style={{
          backgroundColor: color || undefined,
          borderRadius: `${cellRoundness}%`,
        }}
      />
    );
  });

  return (
    <div className="board-wrapper">
      <div className="board-title">
        <span>{title}</span>
        <span style={{ marginLeft: "1rem" }}>Score: {score}</span>
        <span style={{ marginLeft: "1rem" }}>High Score: {highScore}</span>
        <button
          style={{ marginLeft: "1rem", padding: "0.2rem 0.5rem" }}
          onClick={onRestart}
        >
          Restart
        </button>
      </div>
      <div className="board" style={boardStyle}>
        {cellElements}
      </div>
    </div>
  );
}
