#include <iostream>
#include <fstream>
#include <vector>
#include <cmath>

class Point {
private:
    int x;
    int y;

public:
    // Конструктор.
    Point(int x, int y) {
        this->x = x;
        this->y = y;
    }

    // Геттер для х.
    int getX() const {
        return this->x;
    }

    // Геттер для у.
    int getY() const {
        return this->y;
    }

    // Возвращает величину, пропорциональную расстоянию между точек.
    double getFakeDistance(Point& other) const {
        return (this->x - other.x) * (this->x - other.x) + (this->y - other.y) * (this->y - other.y);
    }

    // Возвращает угол
    double getAngle(Point& other) const {
        return std::atan2(this->y - other.y, this->x - other.x);
    }

    // Возвращает строковое представление.
    explicit operator std::string() const {
        return std::to_string(x) + " " + std::to_string(y);
    }
};

class Stack {
private:
    std::vector<Point> points;
    const int MAX_CAPACITY = 1000;
    int capacity;
    int size;
public:
    explicit Stack(int capacity) {
        this->capacity = capacity < MAX_CAPACITY ? capacity : MAX_CAPACITY;
        this->size = 0;
    }

    // Геттер для текущего размера стека.
    int getSize() const{
        return size;
    }

    // Добавление элемента в стек.
    void push(Point point) {
        if (this->size == this->capacity)
            throw std::overflow_error("Stack overflow");
        this->points.push_back(point);
        size++;
    }

    // Удаляет элемент из стека
    Point pop(){
        if (this->size == 0)
            throw std::underflow_error("Stack underflow");
        Point temp = this->points[this->size - 1];
        this->points.pop_back();
        size--;
        return temp;
    }

    // Возвращает последний элемент стека
    Point top() const{
        if (this->size == 0)
            throw std::underflow_error("Stack underflow");
        return this->points[this->size - 1];
    }

    // Возвращает предпослений элемент стека
    Point nextToTop() const{
        if (this->size < 2)
            throw std::underflow_error("Not enough elements");
        return this->points[this->size - 2];
    }

    // Пуст ли стек
    __unused bool isEmpty() const {
        return this->size == 0;
    }

    // Геттер для списка точек
    std::vector<Point> getPoints() const {
        return this->points;
    }

};

static Point Minimum(0,0);

// Читаем точки
static std::vector<Point> readPoints(const std::string& path) {
    std::ifstream fin(path,std::ios_base::in);
    int n;
    fin >> n;

    std::vector<Point> points;
    for (int i = 0; i < n; ++i) {
        int x, y;
        fin >> x >> y;
        points.emplace_back(x, y);
    }

    return points;
}

// Записываем точки в формате WKT
static std::string toWKTMultiPoint(std::vector<Point>& points)
{
    std::string res = "MULTIPOINT (";
    for (int i = 0; i < points.size() - 1; i++)
        res += "(" + std::string(points[i]) + "), ";
    res += "(" + std::string(points[points.size() - 1]) + "))";
    return res;
}

// Записываем точки в формате WKT
static std::string toWKTPolygon(std::vector<Point>& points)
{
    std::string res = "POLYGON ((";
    for (int i = 0; i < points.size() - 1; i++)
        res += std::string(points[i]) + ", ";
    res += std::string(points[points.size() - 1]) + "))";
    return res;
}

// Ищем минимальную точку
static int getMinimalPointPosition(std::vector<Point>& points) {
    int min = 0;

    for (int i = 1; i < points.size(); i++) {
        if (points[i].getY() < points[min].getY() ||
            points[i].getY() == points[min].getY() && points[i].getX() < points[min].getX()) {
            min = i;
        }
    }

    return min;
}

// Сравниваем точки по углу и расстоянию
static bool compareByAngle(Point& p1, Point& p2) {
    if (p1.getAngle(Minimum) == p2.getAngle(Minimum))
        return p1.getFakeDistance(Minimum) < p2.getFakeDistance(Minimum);
    return p1.getAngle(Minimum) < p2.getAngle(Minimum);
}

// Образуется ли поворот влево
static bool leftRotation(Point p1, Point p2, Point p3)
{
    Point vec1(p2.getX() - p1.getX(), p2.getY() - p1.getY());
    Point vec2(p3.getX() - p2.getX(), p3.getY() - p2.getY());

    return vec1.getX() * vec2.getY() - vec1.getY() * vec2.getX() <= 0;
}

// Реализация алгоритма Грехема
static std::vector<Point> startGrahamScan(std::vector<Point> points)
{
    std::vector<Point> sorted;

    // Начало в самой левой нижней точке
    // Добавляем ее в отсортированный список и удаляем из исходного
    int minPos = getMinimalPointPosition(points);
    Minimum = points[minPos];
    sorted.push_back(Minimum);
    points.erase(points.begin() + minPos);

    // Сортируем оставшиеся точки по полярным углам относительно первой
    std::sort(points.begin(), points.end(), compareByAngle);
    sorted.insert(sorted.end(), points.begin(), points.end());

    // Создаем стек
    Stack stack(sorted.size());
    stack.push(sorted[0]);
    stack.push(sorted[1]);

    // Перебираем точки
    for (int i = 2; i < sorted.size(); i++)
    {
        while (stack.getSize() >= 2 &&
               leftRotation(stack.nextToTop(), stack.top(), sorted[i]))
            stack.pop();

        stack.push(sorted[i]);
    }

    // Теперь в стеке лежат нужные нам точки.
    return stack.getPoints();
}

int main(int argc, char* argv[]) {
    // Проверяем количество вргументов
    if (argc != 5)
    {
        std::cout << "Please enter correct arguments";
        std::cout << "Usage: HomeWork2 <direction> <input format> <input file path> <output file path>";
        return 0;
    }

    // Пути к файлам.
    std::string inputPath = std::string(argv[3]);
    std::string outputPath = std::string(argv[4]);

    // Читаем точки.
    std::vector<Point> points = readPoints(inputPath);

    std::string res;

    // Записываем входные данные в WKT.
    if(std::string(argv[2]) == "wkt")
        res = toWKTMultiPoint(points) + "\n";

    // Ищем вешины многоугольника
    std::vector<Point> polygon = startGrahamScan(points);

    // Переворачиваем против часовой стрелки
    if (std::string(argv[1]) == "cw")
        std::reverse(polygon.begin() + 1, polygon.end());

    if(std::string(argv[2]) == "wkt")
        res += toWKTPolygon(polygon);
    else {
        res = std::to_string(polygon.size()) + "\n";
        for (int i = 0; i < polygon.size() - 1; ++i) {
            res += std::string(polygon[i]) + "\n";
        }
        res += std::string(polygon[polygon.size() - 1]);
    }

    // Записываес результат
    std::ofstream fout(outputPath,std::ios_base::out);
    fout << res;

    return 0;
}


