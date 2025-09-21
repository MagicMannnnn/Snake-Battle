import { useEffect, useState, useRef, forwardRef, useImperativeHandle } from "react";
import SnakeBoard from "./SnakeBoard";

function speedToInterval(speed) {
  const clamped = Math.min(Math.max(speed, 1), 120);
  const minInterval = 50;
  const maxInterval = 400;
  const t = (clamped - 1) / 99;
  return Math.round(minInterval * Math.pow(maxInterval / minInterval, 1 - t));
}

const AIGame = forwardRef(({ cols, rows, speed, cellRoundness }, ref) => {
  const [snake, setSnake] = useState([]);
  const [food, setFood] = useState([0, 0]);
  const [score, setScore] = useState(0);
  const [highScore, setHighScore] = useState(() => {
    return parseInt(localStorage.getItem("aiHighScore") || "0", 10);
  });

  const wsRef = useRef(null);
  const pollRef = useRef(null);
  const reconnectRef = useRef(null);

  const connect = () => {
    const ws = new WebSocket("ws://localhost:5000/ws/");
    wsRef.current = ws;

    ws.onopen = () => {
      clearTimeout(reconnectRef.current);
      pollRef.current = setInterval(() => {
        if (ws.readyState === WebSocket.OPEN) ws.send("getdata");
      }, speedToInterval(speed));
    };

    ws.onmessage = (event) => {
      const data = JSON.parse(event.data);
      setSnake(data.snake.map((seg) => [seg.x, seg.y]));
      setFood([data.apple.x, data.apple.y]);
      setScore(data.score);
      setHighScore(prev => {
        const updatedHigh = Math.max(prev, data.score);
        localStorage.setItem("aiHighScore", updatedHigh);
        return updatedHigh;
      });
    };

    ws.onclose = () => {
      clearInterval(pollRef.current);
      reconnectRef.current = setTimeout(connect, 1000);
    };

    ws.onerror = () => ws.close();
  };

  // Expose restart to parent
  const restart = () => {
    if (wsRef.current && wsRef.current.readyState === WebSocket.OPEN) {
      wsRef.current.send("restart");
    }
  };

  useImperativeHandle(ref, () => ({ restart }));

  useEffect(() => {
    connect();
    return () => {
      clearInterval(pollRef.current);
      clearTimeout(reconnectRef.current);
      if (wsRef.current) wsRef.current.close();
    };
  }, []);

  useEffect(() => {
    if (!pollRef.current || !wsRef.current || wsRef.current.readyState !== WebSocket.OPEN) return;
    clearInterval(pollRef.current);
    pollRef.current = setInterval(() => {
      if (wsRef.current.readyState === WebSocket.OPEN) wsRef.current.send("getdata");
    }, speedToInterval(speed));
    return () => clearInterval(pollRef.current);
  }, [speed]);

  return (
    <SnakeBoard
      snake={snake}
      food={food}
      rows={rows}
      cols={cols}
      cellRoundness={cellRoundness}
      title="AI"
      score={score}
      highScore={highScore}
      onRestart={restart}
    />
  );
});

export default AIGame;
