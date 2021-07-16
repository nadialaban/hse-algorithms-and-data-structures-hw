#include <iostream>
#include <vector>
#include<fstream>

class BTree;

const char sep =
#ifdef _WIN32
        '\\';
#else
        '/';
#endif

class BTreeNode {
    std::vector<int> keys;          // Ключи.
    std::vector<int> values;        // Значения.

    int id;                         // Идентификатор узла.
    std::vector<int> children;      // Индентификаторы дочерних узлов.

    int n = 0;                      // Текущее оличество ключей.

    bool leaf = true;               // Является ли узел листом дерева.

public:
    explicit BTreeNode(int _id, std::string &path) {
        id = _id;
        write(path);
    }

    explicit BTreeNode(int _id) {
        id = _id;
    }

    BTreeNode() {
        id = -1;
    }

    static BTreeNode read(int _id, const std::string &path) {
        auto node = BTreeNode(_id);

        std::ifstream file{path + sep + std::to_string(_id) + ".bin", std::ofstream::binary};

        file.read(reinterpret_cast<char *>(&node.n), sizeof(node.n));
        file.read(reinterpret_cast<char *>(&node.leaf), sizeof(node.leaf));

        int item;
        if (!node.leaf) {
            file.read(reinterpret_cast<char *>(&item), sizeof(item));
            node.children.push_back(item);
        }
        for (int i = 0; i < node.n; ++i) {
            file.read(reinterpret_cast<char *>(&item), sizeof(item));
            node.keys.push_back(item);
            file.read(reinterpret_cast<char *>(&item), sizeof(item));
            node.values.push_back(item);
            if (!node.leaf) {
                file.read(reinterpret_cast<char *>(&item), sizeof(item));
                node.children.push_back(item);
            }

        }

        file.close();
        return node;
    }

    void write(const std::string &path) {
        std::ofstream file{path + sep + std::to_string(id) + ".bin",
                           std::ofstream::out | std::ofstream::binary};

        // Записываем количество ключей.
        file.write(reinterpret_cast<const char *>(&n), sizeof(n));
        file.write(reinterpret_cast<const char *>(&leaf), sizeof(leaf));
        if (!leaf)
            file.write(reinterpret_cast<const char *>(&children[0]), sizeof(children[0]));
        for (int i = 0; i < n; ++i) {
            file.write(reinterpret_cast<const char *>(&keys[i]), sizeof(keys[i]));
            file.write(reinterpret_cast<const char *>(&values[i]), sizeof(values[i]));

            if (!leaf)
                file.write(reinterpret_cast<const char *>(&children[i + 1]), sizeof(children[i + 1]));
        }
        file.close();
    }

    void display() {
        std::cout << id << ":\t";
        for (auto k : keys)
            std::cout << k << "\t";
    }

    friend BTree;
};

class BTree {
    int t;                      // Минимальное количство дочерних узлов.
    std::string path;           // Путь к папке, где хранятся файлы.
public:
    int count;                  // Количество узлов.
    BTreeNode root;             // Корневой узел.

    explicit BTree(std::string &_path, int _t) {
        t = _t;
        path = _path;
        count = 0;
        root = BTreeNode(count++, _path);
    }

    /**
     * Возвращает идентификатор узла, где должен быть ключ.
     * @param key - искомый ключ
     * @return узел и ключ.
     */
    std::pair<BTreeNode, int> search(int key, BTreeNode &current) {
        int i = 0;
        while (i < current.n && key > current.keys[i]) i++;
        if (i < current.n && key == current.keys[i]) {
            auto p = std::pair<BTreeNode, int>(current, i);
            return p;
        } else if (current.leaf) {
            auto p = std::pair<BTreeNode, int>(current, -1);
            return p;
        } else {
            auto tmp = BTreeNode::read(current.children[i], path);
            return search(key, tmp);
        }
    }

    /**
     * Поиск значения в дереве.
     * @param key - искомый ключ.
     * @return Значение, которое хранится по этому ключу.
     */
    std::string find_val(int key) {
        auto p = search(key, root);
        return p.second != -1 ? std::to_string(p.first.values[p.second]) : "null";
    }

    /**
     * Разбиение узла.
     * @param parent - родительский узел.
     * @param index - индекс узла, который надо разбить.
     */
    void split(BTreeNode *parent, int index) {
        // Считываем узел, который надо разбить.
        auto old_child = BTreeNode::read(parent->children[index], path);
        // Создаем новый узед
        auto new_child = BTreeNode(count++, path);
        new_child.leaf = old_child.leaf;

        // Копируем вторую половину узла в новый.
        for (int i = 0; i < t - 1; ++i) {
            new_child.keys.push_back(old_child.keys[i + t]);
            new_child.values.push_back(old_child.values[i + t]);

            if (!old_child.leaf)
                new_child.children.push_back(old_child.children[i + t]);
        }

        if (!old_child.leaf)
            new_child.children.push_back(old_child.children[2 * t - 1]);

        old_child.n = t - 1;
        new_child.n = t - 1;

        // Вставляем новый элемемнт в родительский узел.
        parent->keys.insert(parent->keys.begin() + index, old_child.keys[t - 1]); //-1
        parent->values.insert(parent->values.begin() + index, old_child.values[t - 1]); //-1
        parent->children.insert(parent->children.begin() + index + 1, new_child.id); // +1

        parent->n++;

        // Записываем в файлы.
        old_child.write(path);
        new_child.write(path);
        parent->write(path);
    }

    /**
     * Вставка в незаполненный узел.
     * @param node - узел.
     * @param key - ключ.
     * @param val - значение.
     */
    bool insert_non_full(BTreeNode *node, int key, int val) {
        for (int i = 0; i <= node->n; ++i) {
            // Нашли ключ в узле.
            if (i != node->n && node->keys[i] == key) return false;

            // Нашли место, где ключ должен быть.
            if (i == node->n || node->keys[i] > key) {
                // Если текущий узел - лист, вставляем.
                if (node->leaf) {
                    node->keys.insert(node->keys.begin() + i, key);
                    node->values.insert(node->values.begin() + i, val);
                    node->n++;
                    node->write(path);
                    return true;
                } else {
                    // Если нет - идем проверять дочерний узел.
                    auto child = BTreeNode::read(node->children[i], path);
                    // Если дочерний узел переполнен, расщипляем его.
                    if (child.n == 2 * t - 1) {
                        split(node, i);
                        auto tmp = BTreeNode::read(node->id, path);
                        return insert_non_full(&tmp, key, val);
                    }
                    return insert_non_full(&child, key, val);
                }
            }
        }
        return false;
    }

    /**
     * Вставка в дерево.
     * @param key - ключ.
     * @param val - значение.
     */
    bool insert(int key, int val) {
        // Если корень полностью заполнен, расшщипляем его.
        if (root.n == 2 * t - 1) {
            auto s = BTreeNode(count++, path);
            s.leaf = false;
            s.n = 0;
            s.children.push_back(root.id);
            split(&s, 0);
            root = s;
            root.write(path);
            return insert_non_full(&root, key, val);
        } else {
            return insert_non_full(&root, key, val);
        }
    }


    /**
     * Вспомогательный метод для самодебага
     * Коммент на случай, если забуду удалить, ы
     */
    void display() {
        for (int i = 0; i < count; ++i) {
            auto node = BTreeNode::read(i, path);
            node.display();
            std::cout << "\n";
        }
    }

};


int main(int argc, char *argv[]) {
    // Проверяем количество вргументов
    if (argc != 5) {
        std::cout << "Please enter correct arguments";
        std::cout
                << "Usage: HomeWork5 <minimum count of child nodes> <directory path> <input file path> <output file path>";
        return 0;
    }

    // Пути к файлам.
    std::string dirPath = std::string(argv[2]);
    std::string inputPath = std::string(argv[3]);
    std::string outputPath = std::string(argv[4]);

    // Потоки для работы с файлами
    std::ifstream fin(inputPath);
    std::ofstream fout(outputPath, std::ios_base::out);

    auto tree = BTree(dirPath, std::stoi(argv[1]));

    std::string word;
    int key;
    int val;

    // Считываем и обрабатываем запросы.
    while (fin >> word) {
        if (word == "insert") {
            fin >> key;
            fin >> val;
            fout << (tree.insert(key, val) ? "true" : "false") << '\n';
            // tree.display();
            // std::cout << '\n';
        } else {
            fin >> key;
            fout << tree.find_val(key) << '\n';
        }
    }

    // Чистим папку.
    for (int i = 0; i <= tree.count; ++i) {
        std::string tmp = dirPath + sep + std::to_string(i) + ".bin";
        const char *temp = tmp.c_str();
        remove(temp);
    }

    fout.close();
    fin.close();
    return 0;
}
