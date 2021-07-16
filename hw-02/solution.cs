using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AbuAlLabanNadia_198
{
    class MainClass
    {
        /// <summary>
        /// Считывает точки из массива строк
        /// </summary>
        /// <param name="lines">строки из файла</param>
        /// <returns>список точек</returns>
        public static List<Point> GetPoints(string[] lines)
        {
            int n = int.Parse(lines[0]);
            List<Point> points = new List<Point>();
            for (int i = 1; i < n + 1; i++)
            {
                points.Add(new Point(lines[i].Split(' ')));
            }
            return points;
        }

        /// <summary>
        /// Возвращает список точек в формате WKT
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static string ToWKTMultiPoint(List<Point> points)
        {
            string res = "MULTIPOINT (";
            for (int i = 0; i < points.Count - 1; i++)
            {
                res += $"({points[i]}), ";
            }
            res += $"({points[points.Count - 1]}))";
            return res;
        }

        /// <summary>
        /// Возвращает список вершин многоугольника в формате WKT
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static string ToWKTPolygon(List<Point> points)
        {
            string res = "POLYGON ((";
            for (int i = 0; i < points.Count; i++)
            {
                res += $"{points[i]}, ";
            }
            res += $"{points[0]}))";
            return res;
        }

        /// <summary>
        /// Реализация алгоритма Грэхема
        /// </summary>
        /// <param name="points">точки</param>
        /// <returns>вершины многоугольника</returns>
        public static List<Point> GrahamScan(List<Point> points)
        {
            var sorted = new List<Point>();

            // Начало в самой левой нижней точке
            // Добавляем ее в отсортированный список и удаляем из исходного
            sorted.Add(points.OrderBy(p => p.Y).ThenBy(p => p.X).First());
            points.Remove(sorted[0]);

            // Сортируем оставшиеся точки по полярным углам относительно первой
            sorted.AddRange(points.OrderBy(p => Math.Atan2(p.Y - sorted[0].Y, p.X - sorted[0].X))
                                  .ThenBy(p => p.GetFakeDistance(sorted[0])));

            // Создаем стек
            var stack = new Stack(sorted.Count);
            stack.Push(sorted[0]);
            stack.Push(sorted[1]);

            // Перебираем точки
            for (int i = 2; i < sorted.Count; i++)
            {
                while (stack.Size >= 2 &&
                       LeftRotation(stack.NextToТop, stack.Тop, sorted[i]))
                {
                    stack.Pop();
                }

                stack.Push(sorted[i]);
            }

            // Теперь в стеке лежат нужные нам точки.
            return stack.ToList();
        }

        /// <summary>
        /// Образуется лм поворот влево
        /// </summary>
        /// <param name="p1">Точка раз</param>
        /// <param name="p2">Точка два</param>
        /// <param name="p3">Точка три</param>
        /// <returns>Тру, если образуется, не тру иначе</returns>
        public static bool LeftRotation(Point p1, Point p2, Point p3)
        {
            var vec1 = new Point(p2.X - p1.X, p2.Y - p1.Y);
            var vec2 = new Point(p3.X - p2.X, p3.Y - p2.Y);

            return vec1.X * vec2.Y - vec1.Y * vec2.X <= 0;
        }

        /// <summary>
        /// Переворачивает ответ по часовой стрелке
        /// </summary>
        /// <param name="points">вершины сногоугольника</param>
        /// <returns>вершины многоугольника по часовой стрелке</returns>
        public static List<Point> RotateCW(List<Point> points)
        {
            var lst = new List<Point>();
            lst.Add(points[0]);
            points.RemoveAt(0);
            points.Reverse();
            lst.AddRange(points);

            return lst;
        }

        public static void Main(string[] args)
        {
            // Проверяем корректность аргументов
            if (args.Length != 4)
            {
                Console.WriteLine("Please enter correct arguments");
                Console.WriteLine("Usage: HomeWork2 <direction> <input format> <input file path> <output file path>");
                return;
            }

            // Пути к файлам.
            var inputPath = args[2];
            var outputPath = args[3];

            // Считываем входные данные.
            var points = GetPoints(File.ReadAllLines(inputPath));

            string res = string.Empty;

            // Записываем входные данные в WKT
            if (args[1] == "wkt")
                res = ToWKTMultiPoint(points) + Environment.NewLine;

            // Ищем вершины мнгоугольника
            List<Point> polygon = GrahamScan(points);

            // Переворачиваем, если ответ нужен по часовой стрелке
            if (args[0] == "cw")
                polygon = RotateCW(polygon);

            // Формируем ответ
            if (args[1] == "wkt")
                res += ToWKTPolygon(polygon);
            else
            {
                res += polygon.Count + Environment.NewLine;
                res += string.Join(Environment.NewLine, polygon);
            }

            // Записываем результат.
            File.WriteAllText(outputPath, res);
        }
    }

    /// <summary>
    /// Реализация стека
    /// </summary>
    class Stack
    {
        // Сам стек
        private Point[] points;

        // Максимальная допустимая размерность стека (больше точек,
        // чем можно создать, добавлять в стек не нужно будет)
        public const int MAX_CAPACITY = 1000;

        // Количество элементов
        private int size;
        public int Size => size;

        public Stack(int capacity = MAX_CAPACITY)
        {
            this.points = new Point[capacity <= MAX_CAPACITY ? capacity : MAX_CAPACITY];
        }

        /// <summary>
        /// Добавляет элемент в стек
        /// </summary>
        /// <param name="point">точка</param>
        public void Push(Point point)
        {
            if (Size == points.Length)
            {
                throw new StackOverflowException();
            }

            points[size++] = point;
        }

        /// <summary>
        /// Удаляет вершину стека
        /// </summary>
        /// <returns>Удаленный элемент</returns>
        public Point Pop()
        {
            if (Size == 0)
            {
                throw new InvalidOperationException("Stack is empty");
            }

            Point point = points[--size];
            points[size] = null;

            return point;
        }


        /// <summary>
        /// Показывает вершину стека
        /// </summary>
        /// <returns>Первый элемент на выход в стеке</returns>
        public Point Тop
        {
            get
            {
                if (Size == 0)
                {
                    throw new InvalidOperationException("Stack is empty");
                }

                return points[Size - 1];

            }
        }


        /// <summary>
        /// Показывает предпоследнюю точку стека
        /// </summary>
        /// <returns>Второй элемент на выход в стеке</returns>
        public Point NextToТop
        {
            get
            {
                if (Size < 2)
                {
                    throw new IndexOutOfRangeException();
                }

                return points[Size - 2];
            }
        }

        // Пучт ли стек
        public bool IsEmpty => size == 0;

        public List<Point> ToList()
        {
            var lst = new List<Point>();
            for (int i = 0; i < size; i++)
            {
                lst.Add(points[i]);
            }
            return lst;
        }

    }

    /// <summary>
    /// Точка
    /// </summary>
    class Point
    {
        public Point(string[] values)
        {
            this.x = int.Parse(values[0]);
            this.y = int.Parse(values[1]);
        }

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        int x;
        public int X => x;

        int y;
        public int Y => y;


        /// <summary>
        /// Возвращает (расстояние) между точками
        /// Точность не важна, поэтому не берем корень, чтобы не делать лишних вычислений
        /// Все пропорционально, так что все ок
        /// </summary>
        /// <param name="other">Вторая точка</param>
        /// <returns>Типа расстояние</returns>
        public double GetFakeDistance(Point other)
        {
            return (X - other.X) * (X - other.X) + (Y - other.Y) * (X - other.X);
        }

        public override string ToString()
        {
            return $"{x} {y}";
        }
    }
}
