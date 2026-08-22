// book_catalog.cpp
#include <iostream>
#include <fstream>
#include <string>
#include <vector>
#include <map>
#include <algorithm>
#include <nlohmann/json.hpp>
#include <getopt.h>
#include <zint.h>

using namespace std;
using json = nlohmann::json;

struct Book {
    string title, author, isbn, publisher;
    int year;
};

vector<Book> books;
const string DATA_FILE = "books.json";
const string BARCODE_DIR = "barcodes";

void loadBooks() {
    ifstream f(DATA_FILE);
    if (!f.is_open()) return;
    json j;
    f >> j;
    for (auto& item : j) {
        Book b;
        b.title = item["title"];
        b.author = item["author"];
        b.isbn = item["isbn"];
        b.year = item["year"];
        b.publisher = item["publisher"];
        books.push_back(b);
    }
}

void saveBooks() {
    json j = json::array();
    for (auto& b : books) {
        j.push_back({{"title", b.title}, {"author", b.author}, {"isbn", b.isbn}, {"year", b.year}, {"publisher", b.publisher}});
    }
    ofstream f(DATA_FILE);
    f << setw(2) << j << endl;
}

void addBook(const string& title, const string& author, const string& isbn, int year, const string& publisher) {
    for (auto& b : books) {
        if (b.isbn == isbn) {
            cout << "Book with ISBN " << isbn << " already exists.\n";
            return;
        }
    }
    books.push_back({title, author, isbn, publisher, year});
    saveBooks();
    cout << "✅ Added: \"" << title << "\" by " << author << " (ISBN: " << isbn << ")\n";
}

void listBooks() {
    if (books.empty()) {
        cout << "No books in catalog.\n";
        return;
    }
    cout << "\n📋 All Books:\n";
    for (size_t i = 0; i < books.size(); i++) {
        cout << i+1 << ". " << books[i].title << " (" << books[i].isbn << ") – " << books[i].author << " (" << books[i].year << ")\n";
    }
}

void searchBooks(const string& term) {
    string lower = term;
    transform(lower.begin(), lower.end(), lower.begin(), ::tolower);
    vector<Book> results;
    for (auto& b : books) {
        string t = b.title, a = b.author;
        transform(t.begin(), t.end(), t.begin(), ::tolower);
        transform(a.begin(), a.end(), a.begin(), ::tolower);
        if (t.find(lower) != string::npos || a.find(lower) != string::npos || b.isbn.find(term) != string::npos) {
            results.push_back(b);
        }
    }
    if (results.empty()) {
        cout << "No matching books.\n";
        return;
    }
    cout << "\n🔍 Search results for \"" << term << "\":\n";
    for (auto& b : results) {
        cout << b.title << " – " << b.author << " (" << b.isbn << ")\n";
    }
}

void showBook(const string& isbn) {
    for (auto& b : books) {
        if (b.isbn == isbn) {
            cout << "\n📖 Details for " << isbn << ":\n";
            cout << "Title: " << b.title << "\n";
            cout << "Author: " << b.author << "\n";
            cout << "ISBN: " << b.isbn << "\n";
            cout << "Year: " << b.year << "\n";
            cout << "Publisher: " << b.publisher << "\n";
            string barcodePath = BARCODE_DIR + "/" + isbn + ".png";
            ifstream f(barcodePath);
            if (f.good()) {
                cout << "Barcode: " << barcodePath << "\n";
            } else {
                cout << "Barcode not generated yet.\n";
            }
            return;
        }
    }
    cout << "Book with ISBN " << isbn << " not found.\n";
}

void generateBarcode(const string& isbn) {
    if (isbn.length() != 13 || !all_of(isbn.begin(), isbn.end(), ::isdigit)) {
        cout << "Invalid ISBN. Must be 13 digits.\n";
        return;
    }
    bool found = false;
    for (auto& b : books) {
        if (b.isbn == isbn) { found = true; break; }
    }
    if (!found) {
        cout << "Book with ISBN " << isbn << " not found.\n";
        return;
    }
    system(("mkdir -p " + BARCODE_DIR).c_str());
    // Use Zint to generate EAN-13 barcode
    struct zint_symbol *symbol = ZBarcode_Create();
    if (symbol == nullptr) {
        cout << "Error creating barcode symbol.\n";
        return;
    }
    ZBarcode_Clear(symbol);
    symbol->symbology = BARCODE_EANX;
    symbol->height = 60;
    symbol->whitespace_width = 10;
    symbol->scale = 2;
    string filename = BARCODE_DIR + "/" + isbn + ".png";
    if (ZBarcode_Encode_and_Print(symbol, (unsigned char*)isbn.c_str(), 0, 0, filename.c_str(), 0) != ZINT_ERROR) {
        cout << "✅ Barcode generated: " << filename << "\n";
    } else {
        cout << "Error generating barcode.\n";
    }
    ZBarcode_Delete(symbol);
}

int main(int argc, char* argv[]) {
    loadBooks();
    if (argc < 2) {
        cerr << "Usage: book_catalog <command> [options]\n";
        return 1;
    }
    string cmd = argv[1];
    if (cmd == "add") {
        if (argc < 7) { cerr << "add <title> <author> <isbn> <year> <publisher>\n"; return 1; }
        addBook(argv[2], argv[3], argv[4], stoi(argv[5]), argv[6]);
    } else if (cmd == "list") {
        listBooks();
    } else if (cmd == "search") {
        if (argc < 3) { cerr << "search <term>\n"; return 1; }
        searchBooks(argv[2]);
    } else if (cmd == "show") {
        if (argc < 3) { cerr << "show <isbn>\n"; return 1; }
        showBook(argv[2]);
    } else if (cmd == "barcode") {
        if (argc < 3) { cerr << "barcode <isbn>\n"; return 1; }
        generateBarcode(argv[2]);
    } else {
        cerr << "Unknown command\n";
        return 1;
    }
    return 0;
}
