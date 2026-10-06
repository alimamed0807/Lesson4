using System;
using System.Collections.Generic;
using System.Threading;

class Video
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Size { get; set; }
    public double Downloaded { get; set; }
    public string Status { get; set; } = "Waiting";

    public Video(int id, string name, double size)
    {
        Id = id;
        Name = name;
        Size = size;
    }
}

class Program
{
    static SemaphoreSlim semaphore = new SemaphoreSlim(3, 3);
    static List<Video> videos = new List<Video>();
    static List<Thread> threads = new List<Thread>();

    static bool cancelAll = false;
    static readonly object locker = new object();

    static void Main()
    {
        videos.Add(new Video(1, "mp4_sample_file_25MB.mp4", 25));
        videos.Add(new Video(2, "mp4_sample_file_50MB.mp4", 50));
        videos.Add(new Video(3, "mp4_sample_file_100MB.mp4", 100));
        videos.Add(new Video(4, "Unknown", 0));

        for (int i = 0; i < videos.Count; i++)
        {
            Video video = videos[i];

            Thread thread = new Thread(() => Download(video));
            threads.Add(thread);
            thread.Start();
        }

        while (true)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    cancelAll = true;
                    break;
                }
            }

            DrawScreen();

            bool allFinished = true;

            foreach (var video in videos)
            {
                if (video.Status == "Downloading" || video.Status == "Waiting")
                {
                    allFinished = false;
                    break;
                }
            }

            if (allFinished)
                break;

            Thread.Sleep(200);
        }

        foreach (Thread thread in threads)
        {
            if (thread.IsAlive)
                thread.Join();
        }

        DrawScreen();
    }

    static void Download(Video video)
    {
        semaphore.Wait();

        try
        {
            if (cancelAll)
            {
                video.Status = "Cancelled";
                return;
            }

            video.Status = "Downloading";

            if (video.Size == 0)
            {
                Thread.Sleep(500);
                video.Status = "Error";
                return;
            }

            while (video.Downloaded < video.Size)
            {
                if (cancelAll)
                {
                    video.Status = "Cancelled";
                    return;
                }

                Thread.Sleep(300);

                lock (locker)
                {
                    video.Downloaded += 1.0;

                    if (video.Downloaded > video.Size)
                        video.Downloaded = video.Size;
                }
            }

            video.Status = "Completed";
        }
        finally
        {
            semaphore.Release();
        }
    }

    static void DrawScreen()
    {
        lock (locker)
        {
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    MULTI-THREAD VIDEO DOWNLOADER                           ║");
            Console.WriteLine("║                         Semaphore: MAXIMUM 3                              ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════╣");
            Console.ResetColor();

            Console.WriteLine("║ 1–3  → Downloading                                                         ║");
            Console.WriteLine("║ 4+   → Waiting                                                             ║");
            Console.WriteLine("║ ESC  → Cancel ALL                                                          ║");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine();

            foreach (Video video in videos)
            {
                double percent = video.Size == 0
                    ? 0
                    : video.Downloaded / video.Size * 100;

                Console.Write($"#{video.Id}  ");

                if (video.Status == "Downloading")
                    Console.ForegroundColor = ConsoleColor.Cyan;
                else if (video.Status == "Waiting")
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else if (video.Status == "Completed")
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.Write($"{video.Status,-12}");
                Console.ResetColor();

                Console.Write(" [");

                int barSize = 35;
                int filled = (int)(percent / 100 * barSize);

                Console.Write(new string('█', filled));
                Console.Write(new string('░', barSize - filled));

                Console.Write($"] {percent:0}%");

                if (video.Size > 0)
                    Console.Write($" {video.Downloaded:0.0} MB/{video.Size:0.0} MB");

                Console.WriteLine();
                Console.WriteLine($"      {video.Name}");
                Console.WriteLine();
            }

            int active = 0;
            int waiting = 0;
            int completed = 0;
            int errors = 0;

            foreach (Video video in videos)
            {
                if (video.Status == "Downloading")
                    active++;
                else if (video.Status == "Waiting")
                    waiting++;
                else if (video.Status == "Completed")
                    completed++;
                else if (video.Status == "Error" || video.Status == "Cancelled")
                    errors++;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine(
                $"Aktiv: {active} | Gozleyen: {waiting} | Tamamlanan: {completed} | Xeta: {errors}"
            );

            Console.ResetColor();
        }
    }
}