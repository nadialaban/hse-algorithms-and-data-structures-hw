#include <iostream>
#include <fstream>
#include <cmath>
#include <string>
#include <map>
#include <utility>
#include <ctime>

class Bucket {
private:
    char *arr;
    const char SIZE = 4;
public:
    explicit Bucket() {
        arr = new char[SIZE];
        for (int i = 0; i < SIZE; ++i) {
            arr[i] = 0;
        }
    }

    /**
     * @return index of first blank element or -1 if there no space
     */
    int getBlank() {
        for (int i = 0; i < SIZE; ++i) {
            if (arr[i] == 0)
                return i;
        }
        return -1;
    }

    /**
     * Adds fingerprint to arr
     * @param index - index in arr
     * @param fp - fingerprint
     */
    char insert(char index, char fp) {
        auto tmp = arr[index];
        arr[index] = fp;
        return  tmp;
    }

    /**
     * @param fp - fingerprint
     * @return True if fingerprint is in the bucket
     */
    bool check(char fp) {
        for (int i = 0; i < SIZE; ++i) {
            if (arr[i] == fp)
                return true;
        }
        return false;
    }

    ~Bucket() {
        delete [] arr;
    }
};

class CuckooFilter {
private:
    const double FPR = 0.061;
    static const uint FP_SIZE = 7;
    static const int MAX_NUM_KICKS = 500;
    int m; // Количество бакетов.
    std::hash<std::string> hasher;
    Bucket *buckets;
public:
    explicit CuckooFilter(int n) {
        m = std::ceil((1 + FPR) * n);
        buckets = new Bucket[m];
    }

    /**
     * Hash-function for string
     * Hash should be less than count of buckets
     * @param s - string
     * @return hash
     */
    uint getHash(const std::string& s) {
        return static_cast<unsigned>(hasher(s)) % m;
    }

    /**
     * @param hash - hash of string
     * @return fingerprint
     */
    static char getFingerprint(uint hash) {
        return hash % ((1u << FP_SIZE) - 1) + 1;
    }

    /**
     * Inserts value to filter
     * @param val - value
     */
    void insert(std::string &val) {
        auto i1 = getHash(val);
        auto f = getFingerprint(i1);
        auto i2 = (i1 ^ getHash(std::to_string(f))) % m;

        auto a = buckets[i1].getBlank();
        auto b = buckets[i2].getBlank();

        if (a == -1 && b == -1)
        {
            auto i = random() % 2 == 0 ? i1 : i2;
            char tmp = f;

            for (int j = 0; j < MAX_NUM_KICKS; ++j) {
                auto index = random() % 4;
                tmp = buckets[i].insert(index, tmp);
                i = (i ^ getHash(std::to_string(f))) % m;
                a = buckets[i].getBlank();
                if (a != -1)
                    tmp = buckets[i].insert(a, tmp);
                if (tmp == 0)
                    break;
            }
        }
        else {
            if (a != -1)
                buckets[i1].insert(a, f);
            if (b != -1)
                buckets[i2].insert(b, f);
        }
    }

    /**
     * @param val - value
     * @return True if value in filter
     */
    bool check(const std::string &val) {
        auto i1 = getHash(val);
        auto f = getFingerprint(i1);
        auto i2 = (i1 ^ getHash(std::to_string(f))) % m;

        return buckets[i1].check(f) || buckets[i2].check(f);
    }

    ~CuckooFilter() {
        delete [] buckets;
    }

};

int main(int argc, char* argv[]) {
    std::srand(std::time(0));

    // Проверяем количество вргументов
    if (argc != 3)
    {
        std::cout << "Please enter correct arguments";
        std::cout << "Usage: HomeWork4 <input file path> <output file path>";
        return 0;
    }

    // Пути к файлам.
    std::string inputPath = std::string(argv[1]);
    std::string outputPath = std::string(argv[2]);

    // Потоки для работы с файлами
    std::ifstream fin(inputPath);
    std::ofstream fout(outputPath,std::ios_base::out);

    std::map<std::string, CuckooFilter*> users;
    std::string word;
    std::string user;
    std::string video;
    int len = 0;

    fin >> word;
    fin >> len;
    fout << "Ok";
    while (fin >> word) {
        fin >> user;
        fin >> video;

        if (users.count(user) == 0)
            users[user] = new CuckooFilter(len);

        if (word == "watch") {
            users[user]->insert(video);
            fout << "\nOk";
        } else {
            fout << (users[user]->check(video) ? "\nProbably" : "\nNo");
        }
    }

    fout.close();
    fin.close();
    return 0;
}
