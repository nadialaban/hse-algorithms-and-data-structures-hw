using System;
using System.IO;
using System.Collections.Generic;

namespace AbuAlLabanNadia_198_3
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            // Проверяем корректность аргументов
            if (args.Length != 2)
            {
                Console.WriteLine("Please enter correct arguments");
                Console.WriteLine("Usage: HomeWork1 <input file path> <output file path>");
                return;
            }

            // Пути к файлам.
            var sep = Path.DirectorySeparatorChar;
            var inputPath = $"input{sep}{args[0]}";
            var outputPath = $"output{sep}{args[1]}";

            // Считываем входные данные.
            var line = File.ReadAllText(inputPath);

            // Добавляем разделитель между символами, чтобы свести задачу к подсчету нечетных палиндромов
            line = "|" + string.Join("|", line.ToCharArray()) + "|";
            // Считаем количество четных и нечетных палиндромов.
            int odd, even;
            CountPalindroms(line, out odd, out even);

            // Записываем результат.
            File.WriteAllText(outputPath, $"{odd + even} {even} {odd}");

        }


        private static void CountPalindroms(string line, out int odd, out int even)
        {
            var n = line.Length;

            // Ищем количество палиндромов нечетной длины.
            var radiuses = new List<int>();
            even = 0;
            odd = 0;

            // Обработка первого символа.
            radiuses.Add(1);
            var left = 0;
            var right = 0;


            // Обработка оставшейся строки
            for (int i = 1; i < n; i++)
            {
                int tempLeft, tempRight;
                // Если алгоритм вышел за пределы текущего палиндрома, ищем новый от центра в i
                if (i > right)
                {
                    tempLeft = i;
                    tempRight = i;
                }
                else
                {
                    // Радиус текущего символа больше или равен радиусу зеркального относительно центру палиндрома
                    var j = right + left - i;
                    tempLeft = i - radiuses[j] + 1;
                    tempRight = i + radiuses[j] - 1;

                    if (tempRight > right)
                    {
                        var delta = tempRight - right;
                        tempRight -= delta;
                        tempLeft += delta;
                    }
                }

                // Расширяем границы палиндрома с центром в текущем символе
                while (tempLeft - 1 >= 0 && tempRight + 1 < n &&
                       line[tempLeft - 1] == line[tempRight + 1])
                {
                    tempRight++;
                    tempLeft--;
                }

                // Количество палиндромов с центром в i - половина длины этого палиндрома (округление в потолок)
                radiuses.Add(tempRight - i + 1);
                if (line[i] == '|')
                    even += (tempRight - i + 1) / 2;
                else
                    odd += (tempRight - i + 1) / 2;

                // Обновляем границы текущего палиндрома.
                if (tempRight > right)
                {
                    right = tempRight;
                    left = tempLeft;
                }

            }
        }


    }
}
