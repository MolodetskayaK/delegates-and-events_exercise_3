using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class EnumerableExtensions
{
    public static T GetMax<T>(this IEnumerable<T> collection, Func<T, float> convertToNumber) where T : class
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        if (convertToNumber == null) throw new ArgumentNullException(nameof(convertToNumber));

        using var e = collection.GetEnumerator();
        if (!e.MoveNext()) throw new InvalidOperationException("Коллекция пуста.");

        T maxItem = e.Current;
        float maxValue = convertToNumber(maxItem);

        while (e.MoveNext())
        {
            var item = e.Current;
            if (item == null) continue;

            float value = convertToNumber(item);
            if (value > maxValue)
            {
                maxValue = value;
                maxItem = item;
            }
        }

        return maxItem;
    }
}

public sealed class FileFoundEventArgs : EventArgs
{
    public FileFoundEventArgs(string fileName) => FileName = fileName;
    public string FileName { get; }
    public bool Cancel { get; set; }
}

public sealed class FileSearcher
{
    public event EventHandler<FileFoundEventArgs>? FileFound;

    public void Search(string rootPath, string searchPattern = "*", bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("Путь пустой.", nameof(rootPath));
        if (!Directory.Exists(rootPath)) throw new DirectoryNotFoundException(rootPath);

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var file in Directory.EnumerateFiles(rootPath, searchPattern, option))
        {
            var args = new FileFoundEventArgs(file);
            FileFound?.Invoke(this, args);
            if (args.Cancel) break;
        }
    }
}

public class Program
{
    public static void Main()
    {
        const int maxFilesToProcess = 200;

        Console.Write("Введите путь к каталогу: ");
        var path = Console.ReadLine();

        var found = new List<FileInfo>();
        var searcher = new FileSearcher();

        int count = 0;

        searcher.FileFound += (sender, e) =>
        {
            Console.WriteLine($"FileFound: {e.FileName}");
            found.Add(new FileInfo(e.FileName));

            count++;
            if (count >= maxFilesToProcess)
            {
                Console.WriteLine("Отмена поиска из обработчика события (достигнут лимит файлов).");
                e.Cancel = true;
            }
        };

        try
        {
            searcher.Search(path ?? "");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"Найдено файлов: {found.Count}");

        if (found.Count == 0)
        {
            Console.WriteLine("Максимальный элемент не найден: файлов нет.");
            return;
        }

        var maxFile = found.GetMax(f => (float)f.Length);

        Console.WriteLine();
        Console.WriteLine("Результат GetMax:");
        Console.WriteLine($"Максимальный файл: {maxFile.FullName}");
        Console.WriteLine($"Размер (bytes): {maxFile.Length}");
    }
}
