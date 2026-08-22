# book_catalog.rb
#!/usr/bin/env ruby
require 'json'
require 'optparse'
require 'barby'
require 'barby/outputter/png_outputter'
require 'fileutils'

DATA_FILE = 'books.json'
BARCODE_DIR = 'barcodes'

class Book
  attr_accessor :title, :author, :isbn, :year, :publisher

  def initialize(title, author, isbn, year, publisher)
    @title = title
    @author = author
    @isbn = isbn
    @year = year
    @publisher = publisher
  end

  def to_hash
    { title: @title, author: @author, isbn: @isbn, year: @year, publisher: @publisher }
  end

  def self.from_hash(h)
    new(h['title'], h['author'], h['isbn'], h['year'], h['publisher'])
  end
end

class Catalog
  attr_reader :books

  def initialize
    @books = []
    load
  end

  def load
    if File.exist?(DATA_FILE)
      data = JSON.parse(File.read(DATA_FILE))
      @books = data.map { |h| Book.from_hash(h) }
    end
  end

  def save
    File.write(DATA_FILE, JSON.pretty_generate(@books.map(&:to_hash)))
  end

  def add(title, author, isbn, year, publisher)
    if @books.any? { |b| b.isbn == isbn }
      puts "Book with ISBN #{isbn} already exists."
      return
    end
    book = Book.new(title, author, isbn, year, publisher)
    @books << book
    save
    puts "✅ Added: \"#{title}\" by #{author} (ISBN: #{isbn})"
  end

  def list
    if @books.empty?
      puts "No books in catalog."
      return
    end
    puts "\n📋 All Books:"
    @books.each_with_index do |b, i|
      puts "#{i+1}. #{b.title} (#{b.isbn}) – #{b.author} (#{b.year})"
    end
  end

  def search(term)
    lower = term.downcase
    results = @books.select do |b|
      b.title.downcase.include?(lower) ||
      b.author.downcase.include?(lower) ||
      b.isbn.include?(term)
    end
    if results.empty?
      puts "No matching books."
      return
    end
    puts "\n🔍 Search results for \"#{term}\":"
    results.each { |b| puts "#{b.title} – #{b.author} (#{b.isbn})" }
  end

  def show(isbn)
    book = @books.find { |b| b.isbn == isbn }
    unless book
      puts "Book with ISBN #{isbn} not found."
      return
    end
    puts "\n📖 Details for #{isbn}:"
    puts "Title: #{book.title}"
    puts "Author: #{book.author}"
    puts "ISBN: #{book.isbn}"
    puts "Year: #{book.year}"
    puts "Publisher: #{book.publisher}"
    barcode_path = File.join(BARCODE_DIR, "#{isbn}.png")
    if File.exist?(barcode_path)
      puts "Barcode: #{barcode_path}"
    else
      puts "Barcode not generated yet."
    end
  end

  def generate_barcode(isbn)
    unless isbn.match?(/^\d{13}$/)
      puts "Invalid ISBN. Must be 13 digits."
      return
    end
    book = @books.find { |b| b.isbn == isbn }
    unless book
      puts "Book with ISBN #{isbn} not found."
      return
    end
    require 'barby/barcode/ean_13'
    FileUtils.mkdir_p(BARCODE_DIR)
    begin
      barcode = Barby::EAN13.new(isbn)
      outputter = Barby::PngOutputter.new(barcode)
      outputter.to_png(height: 60, margin: 10, xdim: 2)
      File.open(File.join(BARCODE_DIR, "#{isbn}.png"), 'wb') do |f|
        f.write(outputter.to_png(height: 60, margin: 10, xdim: 2))
      end
      puts "✅ Barcode generated: #{BARCODE_DIR}/#{isbn}.png"
    rescue => e
      puts "Error generating barcode: #{e.message}"
    end
  end
end

options = {}
$command = ARGV.shift
if $command.nil?
  puts "Usage: book_catalog.rb <command> [options]"
  exit 1
end

catalog = Catalog.new

case $command
when 'add'
  if ARGV.size < 5
    puts "add <title> <author> <isbn> <year> <publisher>"
    exit 1
  end
  title, author, isbn, year, publisher = ARGV[0], ARGV[1], ARGV[2], ARGV[3].to_i, ARGV[4]
  catalog.add(title, author, isbn, year, publisher)
when 'list'
  catalog.list
when 'search'
  term = ARGV.shift
  catalog.search(term) if term
when 'show'
  isbn = ARGV.shift
  catalog.show(isbn) if isbn
when 'barcode'
  isbn = ARGV.shift
  catalog.generate_barcode(isbn) if isbn
else
  puts "Unknown command"
end
