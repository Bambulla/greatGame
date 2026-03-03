using System.Diagnostics;

const int Width = 24;
const int Height = 18;
const int TickMs = 120;

var random = new Random();
var highScorePath = Path.Combine(AppContext.BaseDirectory, "highscore.txt");

Console.CursorVisible = false;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var highScore = LoadHighScore(highScorePath);

while (true)
{
    var restart = RunGame(random, ref highScore, highScorePath);
    if (!restart)
    {
        break;
    }
}

Console.ResetColor();
Console.Clear();
Console.WriteLine("Спасибо за игру!");

return;

static bool RunGame(Random random, ref int highScore, string highScorePath)
{
    var snake = new LinkedList<Point>();
    snake.AddFirst(new Point(Width / 2, Height / 2));
    snake.AddLast(new Point(Width / 2 - 1, Height / 2));
    snake.AddLast(new Point(Width / 2 - 2, Height / 2));

    var direction = Direction.Right;
    var queuedDirection = direction;
    var food = TrySpawnFood(random, snake) ?? throw new InvalidOperationException("Не удалось создать еду на пустом поле.");
    var score = 0;
    var paused = false;
    var gameOver = false;
    var won = false;

    var stopwatch = Stopwatch.StartNew();
    long lastTick = 0;

    Render(snake, food, score, highScore, paused, gameOver);

    while (!gameOver)
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(intercept: true).Key;
            if (key == ConsoleKey.Spacebar)
            {
                paused = !paused;
                Render(snake, food, score, highScore, paused, gameOver);
                continue;
            }

            var next = MapKey(key);
            if (next is null)
            {
                continue;
            }

            if (!IsOpposite(direction, next.Value))
            {
                queuedDirection = next.Value;
            }
        }

        if (paused)
        {
            Thread.Sleep(10);
            continue;
        }

        if (stopwatch.ElapsedMilliseconds - lastTick < TickMs)
        {
            Thread.Sleep(1);
            continue;
        }

        lastTick = stopwatch.ElapsedMilliseconds;
        direction = queuedDirection;

        var nextHead = Move(snake.First!.Value, direction);
        var outOfBounds = nextHead.X < 0 || nextHead.X >= Width || nextHead.Y < 0 || nextHead.Y >= Height;
        var willEat = nextHead == food;
        var hitSelf = HitsSnake(nextHead, snake, ignoreTail: !willEat);

        if (outOfBounds || hitSelf)
        {
            gameOver = true;
            break;
        }

        snake.AddFirst(nextHead);

        if (willEat)
        {
            score += 10;

            if (score > highScore)
            {
                highScore = score;
                SaveHighScore(highScorePath, highScore);
            }

            var spawnResult = TrySpawnFood(random, snake);
            if (spawnResult is null)
            {
                won = true;
                gameOver = true;
                break;
            }

            food = spawnResult.Value;
        }
        else
        {
            snake.RemoveLast();
        }

        Render(snake, food, score, highScore, paused, gameOver);
    }

    Render(snake, food, score, highScore, paused, gameOver: true);

    Console.SetCursorPosition(0, Height + 5);
    Console.WriteLine(won
        ? "Победа! Поле заполнено. Enter — заново, Esc — выход."
        : "Игра окончена. Enter — заново, Esc — выход.");

    while (true)
    {
        var key = Console.ReadKey(intercept: true).Key;
        if (key == ConsoleKey.Enter)
        {
            return true;
        }

        if (key == ConsoleKey.Escape)
        {
            return false;
        }
    }
}

static Point? TrySpawnFood(Random random, LinkedList<Point> snake)
{
    var boardArea = Width * Height;
    if (snake.Count >= boardArea)
    {
        return null;
    }

    Point food;
    do
    {
        food = new Point(random.Next(0, Width), random.Next(0, Height));
    }
    while (snake.Contains(food));

    return food;
}

static bool HitsSnake(Point nextHead, LinkedList<Point> snake, bool ignoreTail)
{
    if (!ignoreTail)
    {
        return snake.Contains(nextHead);
    }

    var node = snake.First;
    while (node is not null)
    {
        if (node.Next is null)
        {
            break;
        }

        if (node.Value == nextHead)
        {
            return true;
        }

        node = node.Next;
    }

    return false;
}

static Direction? MapKey(ConsoleKey key) => key switch
{
    ConsoleKey.UpArrow or ConsoleKey.W => Direction.Up,
    ConsoleKey.DownArrow or ConsoleKey.S => Direction.Down,
    ConsoleKey.LeftArrow or ConsoleKey.A => Direction.Left,
    ConsoleKey.RightArrow or ConsoleKey.D => Direction.Right,
    _ => null,
};

static bool IsOpposite(Direction current, Direction next) =>
    (current == Direction.Up && next == Direction.Down) ||
    (current == Direction.Down && next == Direction.Up) ||
    (current == Direction.Left && next == Direction.Right) ||
    (current == Direction.Right && next == Direction.Left);

static Point Move(Point point, Direction direction) => direction switch
{
    Direction.Up => point with { Y = point.Y - 1 },
    Direction.Down => point with { Y = point.Y + 1 },
    Direction.Left => point with { X = point.X - 1 },
    Direction.Right => point with { X = point.X + 1 },
    _ => point,
};

static int LoadHighScore(string path)
{
    if (!File.Exists(path))
    {
        return 0;
    }

    var text = File.ReadAllText(path);
    return int.TryParse(text, out var value) ? value : 0;
}

static void SaveHighScore(string path, int value)
{
    File.WriteAllText(path, value.ToString());
}

static void Render(LinkedList<Point> snake, Point food, int score, int highScore, bool paused, bool gameOver)
{
    Console.SetCursorPosition(0, 0);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"🐍 C# Snake | Счёт: {score} | Рекорд: {highScore}          ");
    Console.ResetColor();

    Console.WriteLine("┌" + new string('─', Width) + "┐");

    for (var y = 0; y < Height; y++)
    {
        Console.Write("│");
        for (var x = 0; x < Width; x++)
        {
            var point = new Point(x, y);
            if (point == snake.First!.Value)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("●");
                Console.ResetColor();
            }
            else if (snake.Contains(point))
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("●");
                Console.ResetColor();
            }
            else if (point == food)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("◉");
                Console.ResetColor();
            }
            else
            {
                Console.Write(" ");
            }
        }

        Console.WriteLine("│");
    }

    Console.WriteLine("└" + new string('─', Width) + "┘");
    Console.WriteLine("Управление: стрелки/WASD, Space — пауза.                    ");

    if (paused)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Пауза                                                      ");
        Console.ResetColor();
    }
    else if (gameOver)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Игра окончена                                              ");
        Console.ResetColor();
    }
    else
    {
        Console.WriteLine("                                                           ");
    }
}

enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

readonly record struct Point(int X, int Y);
