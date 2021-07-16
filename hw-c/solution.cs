using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace HomeWork4
{
    class MainClass
    {
        /// <summary>
        /// Парсит строки с предикатами.
        /// </summary>
        /// <param name="lines">Строки из файла.</param>
        /// <returns>Список предикатов.</returns>
        public static List<Predicate> GetPredicates(string[] lines)
        {
            List<Predicate> predicates = new List<Predicate>();
            for (int i = 2; i < lines.Length; i++)
            {
                predicates.Add(new Predicate(lines[i].Split()));
            }
            return predicates;
        }

        /// <summary>
        /// Формирует словарь таблиц и столбцрв.
        /// </summary>
        /// <returns>Сформированный словарь.</returns>
        public static Dictionary<string, List<string>> GetTablesStructure()
        {
            var dict = new Dictionary<string, List<string>>();

            dict.Add("DimProduct", new List<string>
            {
                "ProductKey", "ProductKey", "ProductAlternateKey", "EnglishProductName", "Color",
                "SafetyStockLevel", "ReorderPoint", "SizeRange", "DaysToManufacture",
                "StartDate"
            });

            dict.Add("DimReseller", new List<string>
            {
                "ResellerKey", "ResellerKey", "ResellerAlternateKey", "Phone", "BusinessType",
                "ResellerName", "NumberEmployees", "OrderFrequency", "ProductLine",
                "AddressLine1", "BankName", "YearOpened"
            });

            dict.Add("DimCurrency", new List<string>
            {
                "CurrencyKey", "CurrencyKey", "CurrencyAlternateKey", "CurrencyName"
            });

            dict.Add("DimPromotion", new List<string>
            {
                "PromotionKey", "PromotionKey", "PromotionAlternateKey", "EnglishPromotionName", "EnglishPromotionType",
                "EnglishPromotionCategory", "StartDate", "EndDate", "MinQty"
            });

            dict.Add("DimSalesTerritory", new List<string>
            {
                "SalesTerritoryKey", "SalesTerritoryKey", "SalesTerritoryAlternateKey", "SalesTerritoryRegion", "SalesTerritoryCountry",
                "SalesTerritoryGroup"
            });

            dict.Add("DimEmployee", new List<string>
            {
                "EmployeeKey", "EmployeeKey", "FirstName", "LastName", "Title",
                "BirthDate", "LoginID", "EmailAddress", "Phone",
                "MaritalStatus", "Gender", "PayFrequency", "VacationHours",
                "SickLeaveHours", "DepartmentName", "StartDate"
            });

            dict.Add("DimDate", new List<string>
            {
                "OrderDateKey", "DateKey", "FullDateAlternateKey", "DayNumberOfWeek", "EnglishDayNameOfWeek",
                "DayNumberOfMonth", "DayNumberOfYear", "WeekNumberOfYear", "EnglishMonthName",
                "MonthNumberOfYear", "CalendarQuarter", "CalendarYear", "CalendarSemester",
                "FiscalQuarter", "FiscalYear", "FiscalSemester"
            });

            return dict;
        }

        /// <summary>
        /// Формирует битмап по таблице измерений.
        /// </summary>
        /// <param name="predicate">Предикат.</param>
        /// <param name="dimPath">Путь к таблице измерений.</param>
        /// <param name="factPath">Путь к таблице фактов с ключами.</param>
        /// <param name="index">Индекс нужного столбца в таблице измерений.</param>
        /// <returns>Сформированный битмап.</returns>
        public static RoaringBitmap CreateBitmapFromDim(Predicate predicate, string dimPath, int index)
        {
            var bitmap = new RoaringBitmap();

            var lines = File.ReadAllLines(dimPath);

            foreach (var line in lines)
            {
                var values = line.Split('|');
                if (predicate.Compare(values[index]))
                {
                    bitmap.Set(int.Parse(values[0]), true);
                }
            }
            return bitmap;
        }

        /// <summary>
        /// Перегняет данные от битмапа по табилце измерений в битмап по основной таблице.
        /// </summary>
        /// <param name="factPath">Путь к основной таблице.</param>
        /// <param name="dimBitmap">Битмап по таблице измерений</param>
        /// <returns>Битмап по основной таблице.</returns>
        public static RoaringBitmap CreateFactBitmapFromDimBitmap(string factPath, RoaringBitmap dimBitmap)
        {
            var bitmap = new RoaringBitmap();

            var lines = File.ReadAllLines(factPath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (dimBitmap.Get(int.Parse(lines[i])))
                {
                    bitmap.Set(i, true);
                }
            }
            return bitmap;
        }

        /// <summary>
        /// Формирует битмап по таблице фактов.
        /// </summary>
        /// <param name="predicate">Предикат.</param>
        /// <param name="factPath">Путь к таблице фактов.</param>
        /// <returns>Сформированный битмап.</returns>
        public static RoaringBitmap CreateBitmapFromFact(Predicate predicate, string factPath)
        {
            var bitmap = new RoaringBitmap();
            var lines = File.ReadAllLines(factPath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (predicate.Compare(lines[i]))
                {
                    bitmap.Set(i, true);
                }

            }

            return bitmap;
        }

        /// <summary>
        /// Побитово умножает все битмапы.
        /// </summary>
        /// <param name="bitmaps">Список битмапов.</param>
        /// <returns>Результат умножения.</returns>
        public static RoaringBitmap BitwizeAnd(List<RoaringBitmap> bitmaps)
        {
            if (bitmaps.Count == 0)
            {
                var res = new RoaringBitmap();
                for (int i = 0; i < 60855; i++)
                {
                    res.Set(i, true);
                }
                return res;
            }

            var res1 = bitmaps[0];

            for (int i = 1; i < bitmaps.Count; i++)
            {
                res1.And(bitmaps[i]);
            }

            return res1;
        }

        /// <summary>
        /// Считывает результирующие столбцы из таблицы фактов.
        /// </summary>
        /// <param name="values">Список результирующих строк.</param>
        /// <param name="bitmap">Битмап,</param>
        /// <param name="tablePath">Путь к таблице фактов.</param>
        public static void GetValuesFromFact(ref List<string>[] values, RoaringBitmap bitmap, string tablePath)
        {
            var lines = File.ReadAllLines(tablePath);
            int j = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (bitmap.Get(i))
                {
                    values[j++].Add(lines[i]);
                }
            }
        }

        /// <summary>
        /// Считывает результируещие столбцы из таблицы измерений.
        /// </summary>
        /// <param name="values">Результирующие значения.</param>
        /// <param name="bitmap">Битмап.</param>
        /// <param name="factPath">Путь к таблице фактов.</param>
        /// <param name="dimPath">Путь к таблице измерений.</param>
        /// <param name="index">Нужный индекс в таблице измерений.</param>
        public static void GetValuesFromDim(ref List<string>[] values, RoaringBitmap bitmap, string factPath, string dimPath, int index)
        {
            var dict = new Dictionary<string, string>();
            var dimLines = File.ReadAllLines(dimPath);
            foreach (var line in dimLines)
            {
                var vals = line.Split('|');
                dict.Add(vals[0], vals[index]);
            }

            var lines = File.ReadAllLines(factPath);
            int j = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (bitmap.Get(i))
                {
                    values[j++].Add(dict[lines[i]]);

                }
            }
        }

        public static void Main(string[] args)
        {
            // Проверяем корректность аргументов
            if (args.Length != 3)
            {
                Console.WriteLine("Please enter correct arguments");
                Console.WriteLine("Usage: ControlHomeWork  <data directory path> <input file path> <output file path>");
                return;
            }

            // Пути к файлам.
            var dataPath = args[0];
            var inputPath = args[1];
            var outputPath = args[2];

            Dictionary<string, List<string>> tables = GetTablesStructure();

            // Считываем входные данные.
            var lines = File.ReadAllLines(inputPath);
            var predicates = GetPredicates(lines);


            // Фильтрация таблиц.
            var dimBitmaps = new Dictionary<string, List<RoaringBitmap>>();
            var bitmaps = new List<RoaringBitmap>();
            var sep = Path.DirectorySeparatorChar;

            // Проходимся по каждлму предикату и создаем по нему битмап.
            foreach (var predicate in predicates)
            {
                if (predicate.Table.StartsWith("Dim"))
                {
                    var dimPath = $"{dataPath}{sep}{predicate.Table}.csv";
                    var index = tables[predicate.Table].IndexOf(predicate.Field) - 1;
                    if (index == -1) index++;
                    if (!dimBitmaps.ContainsKey(predicate.Table))
                        dimBitmaps.Add(predicate.Table, new List<RoaringBitmap>());

                    dimBitmaps[predicate.Table].Add(CreateBitmapFromDim(predicate, dimPath, index));
                }
                else
                {
                    var factPath = $"{dataPath}{sep}FactResellerSales.{predicate.Field}.csv";
                    bitmaps.Add(CreateBitmapFromFact(predicate, factPath));
                }
            }


            foreach (var table in dimBitmaps.Keys)
            {
                var tmp = dimBitmaps[table][0];
                for (int i = 1; i < dimBitmaps[table].Count; i++)
                {
                    tmp.And(dimBitmaps[table][i]);
                }

                var factPath = $"{dataPath}{sep}FactResellerSales.{tables[table][0]}.csv";
                bitmaps.Add(CreateFactBitmapFromDimBitmap(factPath, tmp));
            }

            // Побитово умножаем битмапы друг на друга.
            var res = BitwizeAnd(bitmaps);
            // Достаем нужные столбцы.
            var resultValues = new List<string>[res.Cardinality];
            var columns = lines[0].Split(',');

            for (int i = 0; i < resultValues.Length; i++)
            {
                resultValues[i] = new List<string>();
            }

            foreach (var col in columns)
            {
                if (col.StartsWith("Dim"))
                {
                    var pathParts = col.Split('.');
                    var dimPath = $"{dataPath}{sep}{pathParts[0]}.csv";
                    var factPath = $"{dataPath}{sep}FactResellerSales.{tables[pathParts[0]][0]}.csv";
                    var index = tables[pathParts[0]].IndexOf(pathParts[1]) - 1;
                    if (index == -1) index++;
                    GetValuesFromDim(ref resultValues, res, factPath, dimPath, index);
                }
                else
                {
                    var path = $"{dataPath}{sep}{col}.csv";
                    GetValuesFromFact(ref resultValues, res, path);
                }
            }

            // Записываем результат.
            var strRes = string.Join(Environment.NewLine, Array.ConvertAll(resultValues, line => string.Join("|", line))) + Environment.NewLine;
            File.WriteAllText(outputPath, strRes);
        }
    }

    class Predicate
    {
        public readonly string Table;
        public readonly string Field;
        readonly string valueStr;
        readonly int valueInt;
        readonly string op;

        public Predicate(string[] values)
        {
            string[] adress = values[0].Split('.');
            Table = adress[0];
            Field = adress[1];
            op = values[1];

            valueStr = values[2];
            for (int i = 3; i < values.Length; i++)
            {
                valueStr += " " + values[i];
            }
            valueStr = valueStr.Trim('\'');

            int.TryParse(valueStr, out valueInt);
        }

        /// <summary>
        /// Проверяет проходит ли значение по предикату.
        /// </summary>
        /// <param name="val">Значение.</param>
        /// <returns>Да, если подходит.</returns>
        public bool Compare(string val)
        {
            switch (op)
            {
                case "<>":
                    return !val.Equals(valueStr);
                case "=":
                    return val.Equals(valueStr);
                default:
                    break;
            }

            int val1 = int.Parse(val);

            switch (op)
            {
                case ">":
                    return val1 > valueInt;
                case ">=":
                    return val1 >= valueInt;
                case "<":
                    return val1 < valueInt;
                case "<=":
                    return val1 <= valueInt;
                default:
                    return false;
            }
        }
        public override string ToString()
        {
            return $"{Table}.{Field} {op} {valueStr}";
        }

    }

    abstract class Bitmap
    {
        public abstract void And(Bitmap other);
        public abstract void Set(int i, bool value);
        public abstract bool Get(int i);
    }

    class RoaringBitmap : Bitmap
    {
        private const int CONTAINER_SIZE = 1 << 16;
        private List<Container> containers = new List<Container>();

        /// <summary>
        /// Количество битов, которые есть в битмапе.
        /// </summary>
        public long Cardinality
        {
            get
            {
                int tmp = 0;

                foreach (var container in containers)
                {
                    if (container is null)
                        continue;
                    tmp += container.Cardinality;
                }

                return tmp;
            }
        }

        /// <summary>
        /// Присутствует ли бит в битмапе.
        /// </summary>
        /// <param name="i">Номер бита.</param>
        /// <returns>Истина, если значение присутствует.</returns>
        public override bool Get(int i)
        {
            if (i < containers.Count * CONTAINER_SIZE)
            {
                return !(containers[i >> 16] is null) && containers[i >> 16].Get(i & 0xffff);
            }
            return false;
        }

        /// <summary>
        /// Устанавливает значение бита.
        /// </summary>
        /// <param name="i">Номер бита.</param>
        /// <param name="value">Значение.</param>
        public override void Set(int i, bool value)
        {
            while (i >= containers.Count * CONTAINER_SIZE)
            {
                containers.Add(null);
            }

            var index = i >> 16;
            //var index = i / co;

            if (containers[index] is null)
            {
                if (!value)
                {
                    return;
                }

                containers[index] = new ArrayContainer();
            }

            containers[index].Set(i & 0xffff, value);

            if (containers[index].Cardinality <= 4096 && containers[index] is BitmapContainer ||
                containers[index].Cardinality > 4096 && containers[index] is ArrayContainer)
            {
                containers[index] = ChangeContainer(containers[index]);
            }

        }

        /// <summary>
        /// Побитово умножает битмап на другой битмап.
        /// </summary>
        /// <param name="other">Второй битмап.</param>
        public override void And(Bitmap other)
        {
            for (int i = 0; i < containers.Count; i++)
            {
                if (containers[i] is null)
                    continue;
                for (int j = 0; j < CONTAINER_SIZE; j++)
                {
                    int index = j + i * CONTAINER_SIZE;
                    if (containers[i].Get(j) && !other.Get(index))
                        containers[i].Set(j, false);
                    //containers[i].Set(j, containers[i].Get(j) & other.Get(index));

                    if (containers[i].Cardinality <= 4096 && containers[i] is BitmapContainer)
                    {
                        containers[i] = ChangeContainer(containers[i]);
                    }

                }
            }
        }

        /// <summary>
        /// Изменяет тип контейнера.
        /// </summary>
        /// <param name="container">Контейнер.</param>
        /// <returns>Соответствующий контейнер другого типа.</returns>
        private Container ChangeContainer(Container container)
        {
            Container tmp = default;
            if (container is ArrayContainer array)
            {
                tmp = new BitmapContainer();

                for (int i = 0; i < array.Cardinality; i++)
                {
                    tmp.Set(array.GetValue(i), true);
                    if (tmp.Cardinality == container.Cardinality)
                    {
                        break;
                    }
                }
            }
            else if (container is BitmapContainer bitmap)
            {
                tmp = new ArrayContainer();

                for (var i = 0; i < CONTAINER_SIZE; ++i)
                {
                    if (bitmap.Get(i))
                    {
                        tmp.Set(i, true);
                    }

                    if (tmp.Cardinality == container.Cardinality)
                    {
                        break;
                    }
                }
            }
            return tmp;
        }

    }

    abstract class Container
    {
        public virtual int Cardinality { get; protected set; }

        public abstract void Set(int i, bool value);
        public abstract bool Get(int i);

    }

    class ArrayContainer : Container
    {
        private List<ushort> values { get; } = new List<ushort>();

        public override int Cardinality => values.Count;

        public int GetValue(int i) => values[i];

        /// <summary>
        /// Присутствует ли бит в контейнере.
        /// </summary>
        /// <param name="i">Номер бита.</param>
        /// <returns>Истина, если присутствует.</returns>
        public override bool Get(int i) => values.BinarySearch((ushort)i) >= 0;

        /// <summary>
        /// Устанавливает значение для бита.
        /// </summary>
        /// <param name="i">Номер бита.</param>
        /// <param name="value">Значение.</param>
        public override void Set(int i, bool value)
        {
            if (value)
            {
                if (values.BinarySearch((ushort)i) < 0)
                {
                    values.Add((ushort)i);
                }
            }
            else
            {
                var res = values.BinarySearch((ushort)i);
                if (res >= 0)
                {
                    values.RemoveAt(res);
                }
            }
        }

    }

    class BitmapContainer : Container
    {
        private uint[] values;
        private const int SIZE = 1 << 11;

        public BitmapContainer()
        {
            values = new uint[SIZE];
            Cardinality = 0;
        }

        /// <summary>
        /// Присутствует ли бит в контейнере.
        /// </summary>
        /// <param name="i">Номер бита.</param>
        /// <returns>Истина, если присутствует.</returns>
        public override bool Get(int i) => (values[i >> 5] & (1 << i)) != 0;

        /// <summary>
        /// Устанавливает значение для бита.
        /// </summary>
        /// <param name="i">Номер бита.</param>
        /// <param name="value">Значение.</param>
        public override void Set(int i, bool value)
        {
            if (value)
            {
                values[i >> 5] |= (1u << (i & 0x1f));
                Cardinality++;
            }
            else
            {
                values[i >> 5] &= ~(1u << (i & 0x1f));
                Cardinality--;
            }
        }
    }
}