// BookCatalog.java
import java.io.*;
import java.nio.file.*;
import java.util.*;
import com.google.gson.*;
import org.krysalis.barcode4j.impl.code128.Code128Bean;
import org.krysalis.barcode4j.output.bitmap.BitmapCanvasProvider;

class Book {
    String title, author, isbn, publisher;
    int year;
}

class Catalog {
    List<Book> books = new ArrayList<>();
}

public class BookCatalog {
    private static final String DATA_FILE = "books.json";
    private static final String BARCODE_DIR = "barcodes";
    private static final Gson gson = new GsonBuilder().setPrettyPrinting().create();

    public static void main(String[] args) throws Exception {
        if (args.length < 1) {
            System.out.println("Usage: BookCatalog <command> [options]");
            return;
        }
        Catalog catalog = loadCatalog();
        String cmd = args[0];
        switch (cmd) {
            case "add":
                if (args.length < 6) { System.out.println("add <title> <author> <isbn> <year> <publisher>"); return; }
                addBook(catalog, args[1], args[2], args[3], Integer.parseInt(args[4]), args[5]);
                break;
            case "list":
                listBooks(catalog);
                break;
            case "search":
                if (args.length < 2) { System.out.println("search <term>"); return; }
                searchBooks(catalog, args[1]);
                break;
            case "show":
                if (args.length < 2) { System.out.println("show <isbn>"); return; }
                showBook(catalog, args[1]);
                break;
            case "barcode":
                if (args.length < 2) { System.out.println("barcode <isbn>"); return; }
                generateBarcode(catalog, args[1]);
                break;
            default:
                System.out.println("Unknown command");
        }
    }

    static Catalog loadCatalog() throws IOException {
        Catalog catalog = new Catalog();
        Path path = Paths.get(DATA_FILE);
        if (Files.exists(path)) {
            String json = new String(Files.readAllBytes(path));
            catalog = gson.fromJson(json, Catalog.class);
        }
        return catalog;
    }

    static void saveCatalog(Catalog catalog) throws IOException {
        Files.write(Paths.get(DATA_FILE), gson.toJson(catalog).getBytes());
    }

    static void addBook(Catalog catalog, String title, String author, String isbn, int year, String publisher) throws IOException {
        for (Book b : catalog.books) {
            if (b.isbn.equals(isbn)) {
                System.out.println("Book with ISBN " + isbn + " already exists.");
                return;
            }
        }
        Book book = new Book();
        book.title = title; book.author = author; book.isbn = isbn; book.year = year; book.publisher = publisher;
        catalog.books.add(book);
        saveCatalog(catalog);
        System.out.printf("✅ Added: \"%s\" by %s (ISBN: %s)\n", title, author, isbn);
    }

    static void listBooks(Catalog catalog) {
        if (catalog.books.isEmpty()) {
            System.out.println("No books in catalog.");
            return;
        }
        System.out.println("\n📋 All Books:");
        int i = 1;
        for (Book b : catalog.books) {
            System.out.printf("%d. %s (%s) – %s (%d)\n", i++, b.title, b.isbn, b.author, b.year);
        }
    }

    static void searchBooks(Catalog catalog, String term) {
        String lower = term.toLowerCase();
        List<Book> results = new ArrayList<>();
        for (Book b : catalog.books) {
            if (b.title.toLowerCase().contains(lower) || b.author.toLowerCase().contains(lower) || b.isbn.contains(term)) {
                results.add(b);
            }
        }
        if (results.isEmpty()) {
            System.out.println("No matching books.");
            return;
        }
        System.out.printf("\n🔍 Search results for \"%s\":\n", term);
        for (Book b : results) {
            System.out.printf("%s – %s (%s)\n", b.title, b.author, b.isbn);
        }
    }

    static void showBook(Catalog catalog, String isbn) {
        for (Book b : catalog.books) {
            if (b.isbn.equals(isbn)) {
                System.out.printf("\n📖 Details for %s:\n", isbn);
                System.out.println("Title: " + b.title);
                System.out.println("Author: " + b.author);
                System.out.println("ISBN: " + b.isbn);
                System.out.println("Year: " + b.year);
                System.out.println("Publisher: " + b.publisher);
                String barcodePath = BARCODE_DIR + "/" + isbn + ".png";
                if (Files.exists(Paths.get(barcodePath))) {
                    System.out.println("Barcode: " + barcodePath);
                } else {
                    System.out.println("Barcode not generated yet.");
                }
                return;
            }
        }
        System.out.println("Book with ISBN " + isbn + " not found.");
    }

    static void generateBarcode(Catalog catalog, String isbn) throws Exception {
        if (!isbn.matches("\\d{13}")) {
            System.out.println("Invalid ISBN. Must be 13 digits.");
            return;
        }
        boolean found = false;
        for (Book b : catalog.books) {
            if (b.isbn.equals(isbn)) { found = true; break; }
        }
        if (!found) {
            System.out.println("Book with ISBN " + isbn + " not found.");
            return;
        }
        Files.createDirectories(Paths.get(BARCODE_DIR));
        Code128Bean bean = new Code128Bean();
        bean.setHeight(15d);
        bean.setModuleWidth(0.2);
        bean.setQuietZone(1);
        bean.doQuietZone(true);
        File outputFile = new File(BARCODE_DIR, isbn + ".png");
        try (FileOutputStream fos = new FileOutputStream(outputFile)) {
            BitmapCanvasProvider provider = new BitmapCanvasProvider(fos, "image/png", 300, BufferedImage.TYPE_BYTE_GRAY, false, 0);
            bean.generateBarcode(provider, isbn);
            provider.finish();
        }
        System.out.println("✅ Barcode generated: " + outputFile.getPath());
    }
}
