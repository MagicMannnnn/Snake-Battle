import { useEffect, useState, useRef, forwardRef, useImperativeHandle } from "react";
import SnakeGame from "./SnakeGame";
import SnakeBoard from "./SnakeBoard";

function speedToInterval(speed) {
  const clamped = Math.min(Math.max(speed, 1), 100);
  const minInterval = 50;
  const maxInterval = 400;
  const t = (clamped - 1) / 99;
  return Math.round(minInterval * Math.pow(maxInterval / minInterval, 1 - t));
}

const HumanGame = forwardRef(({ cols, rows, speed, cellRoundness }, ref) => {
  const [snake, setSnake] = useState([]);
  const [food, setFood] = useState([]);
  const [score, setScore] = useState(0);
  const [highScore, setHighScore] = useState(() => {
    return parseInt(localStorage.getItem("humanHighScore") || "0", 10);
  });

  const gameRef = useRef(null);
  const prevSnakeLength = useRef(3);
  const intervalRef = useRef(null);

  // Initialize game
  const restart = () => {
    const game = new SnakeGame(cols, rows);
    gameRef.current = game;
    setSnake(game.getSnake());
    setFood(game.getFood());
    setScore(0);
    prevSnakeLength.current = 3;
  };

  useImperativeHandle(ref, () => ({ restart }));

  // Start game loop
  useEffect(() => {
    restart();
  }, [cols, rows]);

  useEffect(() => {
    clearInterval(intervalRef.current);
    intervalRef.current = setInterval(() => {
      if (!gameRef.current) return;
      const game = gameRef.current;
      game.step();
      const newSnake = [...game.getSnake()];
      setSnake(newSnake);
      setFood([...game.getFood()]);

      if (newSnake.length > prevSnakeLength.current) {
        setScore((prev) => {
          const newScore = prev + 1;
          setHighScore((prevHigh) => {
            const updatedHigh = Math.max(prevHigh, newScore);
            localStorage.setItem("humanHighScore", updatedHigh);
            return updatedHigh;
          });
          return newScore;
        });
        prevSnakeLength.current = newSnake.length;
      }
    }, speedToInterval(speed));

    return () => clearInterval(intervalRef.current);
  }, [speed]);

  // Save high score
  useEffect(() => {
    localStorage.setItem("snakeHighScore", highScore);
  }, [highScore]);

  // Key controls
  useEffect(() => {
    const handleKey = (e) => {
      if (!gameRef.current) return;
      switch (e.code) {
        case "KeyW":
        case "ArrowUp": gameRef.current.setDirection(0, -1); break;
        case "KeyS":
        case "ArrowDown": gameRef.current.setDirection(0, 1); break;
        case "KeyA":
        case "ArrowLeft": gameRef.current.setDirection(-1, 0); break;
        case "KeyD":
        case "ArrowRight": gameRef.current.setDirection(1, 0); break;
      }
    };
    window.addEventListener("keydown", handleKey);
    return () => window.removeEventListener("keydown", handleKey);
  }, []);

  return (
    <SnakeBoard
      snake={snake}
      food={food}
      rows={rows}
      cols={cols}
      cellRoundness={cellRoundness}
      title="Human"
      score={score}
      highScore={highScore}
      onRestart={restart}
    />
  );
});

export default HumanGame;
