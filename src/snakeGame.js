export default class SnakeGame {
  constructor(cols, rows) {
    this.cols = cols;
    this.rows = rows;
    this.reset();
  }

  reset() {
    // Snake starts in the center
    this.snake = [[Math.floor(this.cols / 2), Math.floor(this.rows / 2)]];
    this.direction = [0, -1]; // moving up initially
    this.nextDirection = [0, -1];
    this.grow = 2; // how many times to grow initially
    this.alive = true;

    // Place the first food
    this.food = this.randomFood();
  }

  getSnake() {
    return this.snake;
  }

  getFood() {
    return this.food;
  }

  isAlive() {
    return this.alive;
  }

  setDirection(dx, dy) {
    // Prevent reversing direction
    const [cx, cy] = this.direction;
    if (dx === -cx && dy === -cy) return;
    this.nextDirection = [dx, dy];
  }

  step() {
    if (!this.alive) return;

    this.direction = this.nextDirection;

    const [dx, dy] = this.direction;
    const [headX, headY] = this.snake[0];
    const newHead = [headX + dx, headY + dy];

    // Check collisions
    if (
      newHead[0] < 0 ||
      newHead[0] >= this.cols ||
      newHead[1] < 0 ||
      newHead[1] >= this.rows ||
      this.snake.some(([x, y]) => x === newHead[0] && y === newHead[1])
    ) {
      this.alive = false;
      return;
    }

    this.snake.unshift(newHead); // add new head

    // Eating food
    if (newHead[0] == this.food[0] && newHead[1] == this.food[1]) {
      this.grow += 1;
      this.food = this.randomFood();
    }

    // Remove tail if not growing
    if (this.grow > 0) {
      this.grow -= 1;
    } else {
      this.snake.pop();
    }
  }

  randomFood() {
    let pos;
    do {
      pos = [Math.floor(Math.random() * this.cols), Math.floor(Math.random() * this.rows)];
    } while (this.snake.some(([x, y]) => x === pos[0] && y === pos[1]));
    return pos;
  }
}
