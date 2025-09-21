import { useState, useRef } from "react";
import HumanGame from "./HumanGame";
import AIGame from "./AIGame";
import "./App.css";

export default function App() {
  const cols = 20;
  const rows = 20;

  const [cellRoundness, setCellRoundness] = useState(10);
  const [speed, setSpeed] = useState(65);
  const [aiSpeed, setAiSpeed] = useState(65);

  // Refs to trigger restart in child games
  const humanRef = useRef(null);
  const aiRef = useRef(null);

  const handleBattle = () => {
    // Trigger restart in child games
    humanRef.current?.restart();
    aiRef.current?.restart();
  };

  return (
    <div className="container">
      <div className="header">
        <button
          onClick={handleBattle}
          className="battle-button"
        >
          Snake Battle
        </button>
      </div>

      <div className="boards">
        <HumanGame
          ref={humanRef}
          cols={cols}
          rows={rows}
          speed={speed}
          cellRoundness={cellRoundness}
        />
        <AIGame
          ref={aiRef}
          cols={cols}
          rows={rows}
          speed={aiSpeed}
          cellRoundness={cellRoundness}
        />
      </div>

      <div className="slider-container">
        <div className="slider-block">
          <label htmlFor="speed">Speed: {speed}</label>
          <input
            id="speed"
            type="range"
            min="1"
            max="100"
            value={speed}
            onChange={(e) => setSpeed(Number(e.target.value))}
          />
        </div>

        <div className="slider-block">
          <label htmlFor="roundness">Cell Roundness</label>
          <input
            id="roundness"
            type="range"
            min="0"
            max="50"
            value={cellRoundness}
            onChange={(e) => setCellRoundness(Number(e.target.value))}
          />
        </div>

        <div className="slider-block">
          <label htmlFor="aispeed">Speed: {aiSpeed}</label>
          <input
            id="aispeed"
            type="range"
            min="1"
            max="120"
            value={aiSpeed}
            onChange={(e) => setAiSpeed(Number(e.target.value))}
          />
        </div>

      </div>
    </div>
  );
}
