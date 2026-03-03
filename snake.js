const BOARD_SIZE = 24;
const CELL_SIZE = 20;
const TICK_MS = 120;

const board = document.getElementById('board');
const ctx = board.getContext('2d');
const scoreElement = document.getElementById('score');
const highScoreElement = document.getElementById('high-score');
const restartButton = document.getElementById('restart');

const baseHeadColor = '#8efc89';
const bodyColor = '#5ecf68';
const foodColor = '#ff7f50';
const gameOverOverlay = 'rgba(0, 0, 0, 0.5)';

let snake;
let direction;
let queuedDirection;
let food;
let score;
let highScore;
let gameLoop;
let paused;
let gameOver;

function randomCell() {
  return {
    x: Math.floor(Math.random() * BOARD_SIZE),
    y: Math.floor(Math.random() * BOARD_SIZE),
  };
}

function spawnFood() {
  let candidate = randomCell();
  while (snake.some((segment) => segment.x === candidate.x && segment.y === candidate.y)) {
    candidate = randomCell();
  }
  food = candidate;
}

function resetGame() {
  snake = [
    { x: 12, y: 12 },
    { x: 11, y: 12 },
    { x: 10, y: 12 },
  ];
  direction = { x: 1, y: 0 };
  queuedDirection = direction;
  score = 0;
  paused = false;
  gameOver = false;
  spawnFood();
  updateScore();

  if (gameLoop) {
    clearInterval(gameLoop);
  }

  gameLoop = setInterval(step, TICK_MS);
  draw();
}

function updateScore() {
  scoreElement.textContent = String(score);
  highScoreElement.textContent = String(highScore);
}

function setHighScoreIfNeeded() {
  if (score > highScore) {
    highScore = score;
    localStorage.setItem('snake-high-score', String(highScore));
    updateScore();
  }
}

function collideWithSelf(head) {
  return snake.some((segment) => segment.x === head.x && segment.y === head.y);
}

function step() {
  if (paused || gameOver) {
    return;
  }

  direction = queuedDirection;

  const head = {
    x: snake[0].x + direction.x,
    y: snake[0].y + direction.y,
  };

  const hitsWall = head.x < 0 || head.y < 0 || head.x >= BOARD_SIZE || head.y >= BOARD_SIZE;
  if (hitsWall || collideWithSelf(head)) {
    gameOver = true;
    setHighScoreIfNeeded();
    draw();
    return;
  }

  snake.unshift(head);

  const ateFood = head.x === food.x && head.y === food.y;
  if (ateFood) {
    score += 10;
    setHighScoreIfNeeded();
    spawnFood();
  } else {
    snake.pop();
  }

  updateScore();
  draw();
}

function drawGrid() {
  ctx.clearRect(0, 0, board.width, board.height);
  ctx.fillStyle = '#0c140b';
  ctx.fillRect(0, 0, board.width, board.height);

  ctx.strokeStyle = 'rgba(255, 255, 255, 0.05)';
  ctx.lineWidth = 1;
  for (let i = 1; i < BOARD_SIZE; i += 1) {
    const p = i * CELL_SIZE;
    ctx.beginPath();
    ctx.moveTo(p, 0);
    ctx.lineTo(p, board.height);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(0, p);
    ctx.lineTo(board.width, p);
    ctx.stroke();
  }
}

function drawSnake() {
  snake.forEach((segment, index) => {
    ctx.fillStyle = index === 0 ? baseHeadColor : bodyColor;
    ctx.fillRect(segment.x * CELL_SIZE + 1, segment.y * CELL_SIZE + 1, CELL_SIZE - 2, CELL_SIZE - 2);
  });
}

function drawFood() {
  const padding = 4;
  ctx.fillStyle = foodColor;
  ctx.beginPath();
  ctx.roundRect(
    food.x * CELL_SIZE + padding,
    food.y * CELL_SIZE + padding,
    CELL_SIZE - padding * 2,
    CELL_SIZE - padding * 2,
    6,
  );
  ctx.fill();
}

function drawStatusOverlay(text) {
  ctx.fillStyle = gameOverOverlay;
  ctx.fillRect(0, 0, board.width, board.height);
  ctx.fillStyle = '#fff';
  ctx.font = 'bold 32px Inter, sans-serif';
  ctx.textAlign = 'center';
  ctx.fillText(text, board.width / 2, board.height / 2);
}

function draw() {
  drawGrid();
  drawFood();
  drawSnake();

  if (paused) {
    drawStatusOverlay('Пауза');
  }

  if (gameOver) {
    drawStatusOverlay('Игра окончена');
  }
}

function isOppositeDirection(next) {
  return next.x === -direction.x && next.y === -direction.y;
}

function mapKeyToDirection(key) {
  const map = {
    ArrowUp: { x: 0, y: -1 },
    KeyW: { x: 0, y: -1 },
    ArrowDown: { x: 0, y: 1 },
    KeyS: { x: 0, y: 1 },
    ArrowLeft: { x: -1, y: 0 },
    KeyA: { x: -1, y: 0 },
    ArrowRight: { x: 1, y: 0 },
    KeyD: { x: 1, y: 0 },
  };
  return map[key];
}

document.addEventListener('keydown', (event) => {
  if (event.code === 'Space') {
    paused = !paused;
    draw();
    return;
  }

  if (gameOver) {
    return;
  }

  const nextDirection = mapKeyToDirection(event.code);
  if (!nextDirection || isOppositeDirection(nextDirection)) {
    return;
  }

  queuedDirection = nextDirection;
});

restartButton.addEventListener('click', resetGame);

highScore = Number(localStorage.getItem('snake-high-score') || 0);
updateScore();
resetGame();
