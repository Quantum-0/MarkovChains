using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Citrina;
using System.IO;
using Markov;
using System.Diagnostics;

namespace VKMessagesDump
{
    class Program
    {
        static CitrinaClient client;
        static IAccessToken token;

        static void Main(string[] args)
        {

            // https://oauth.vk.com/authorize?client_id=4025857&display=page&redirect_uri=https://oauth.vk.com/blank.html&scope=messages,offline&response_type=token&v=5.52
            var tokenText = "PUT ACCESS TOKEN HERE";
            client = new CitrinaClient();
            var fs = new FileStream("output.txt", FileMode.Append, FileAccess.Write, FileShare.Read);
            var sw = new StreamWriter(fs);
            object sync = new object();

            var gen = new TrigramMarkovGenerator();

            token = new UserAccessToken(tokenText, 64800, 20108853, 4025857);

            byte mergingqueue = 0;
            var test = client.Messages.GetDialogs(new Citrina.StandardApi.Models.MessagesGetDialogsRequest() { AccessToken = token, Count = 100, PreviewLength = 100 });
            var test2 = test.Result;
            var test3 = test2.Response.Items.Select(i => i.Message).Where(m => m.ChatId != null).ToArray();
            foreach (var item in test3)
            {
                var test4 = client.Messages.GetHistory(new Citrina.StandardApi.Models.MessagesGetHistoryRequest() { AccessToken = token, PeerId = 2000000000 + item.ChatId, Count = 1 }).Result.Response;
                var count = test4.Count ?? 0;
                Task.Delay(200).Wait();
                int messages = 0;
                Stopwatch stw = new Stopwatch();
                stw.Start();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Processing: " + item.Title + " (" + count +" messages)");
                Console.ForegroundColor = ConsoleColor.Gray;

                int pos = Console.CursorTop;
                Console.WriteLine("[                    ] 0%");
                int temppos;

                for (int i = 0; i < count / 200; i++)
                {
                    Task.Delay(333).Wait();
                    var test5 = client.Messages.GetHistory(new Citrina.StandardApi.Models.MessagesGetHistoryRequest() { AccessToken = token, PeerId = 2000000000 + item.ChatId, Rev = 1, Count = 200, Offset = 200 * i }).Result;
                    var test6 = test5.Response.Items.Select(m => m.Body).Where(m => !string.IsNullOrWhiteSpace(m) && m.Length > 10).ToArray();
                    lock (sync)
                    {
                        Console.Write((1 + i).ToString() + ")" + test6.Length.ToString() + " messages readed");
                    }
                    var tempgen = new TrigramMarkovGenerator();
                    messages += test6.Length;
                    foreach (var msg in test6)
                    {
                        sw.WriteLine(msg);
                        tempgen.LearnText(msg);
                    }
                    lock (sync)
                        Console.Write(", " + (tempgen.GetNGramCount(3) + tempgen.GetNGramCount(2) + tempgen.GetNGramCount(1)) + " n-grams parsed");

                    var left = Console.CursorLeft;
                    var top = Console.CursorTop;

                    Console.WriteLine();
                    mergingqueue++;

                    Task.Run(() =>
                    {
                        gen.Union(tempgen);
                        lock (sync)
                        {
                            int temp = Console.CursorTop;
                            Console.CursorTop = top;
                            top = temp;
                            temp = Console.CursorLeft;
                            Console.CursorLeft = left;
                            left = temp;
                            Console.Write(", merged.");
                            Console.CursorTop = top;
                            Console.CursorLeft = left;
                        }
                        --mergingqueue;
                    });

                    lock (sync)
                    {
                        temppos = Console.CursorTop;
                        Console.SetCursorPosition(0, pos);
                        int partof20 = (i + 1 == count / 200) ? 20 : (i + 1) * 200 * 20 / count;
                        Console.WriteLine("[{0}{1}] {2}%", new string('█', partof20), new string(' ', 20 - partof20), partof20 * 5);
                        Console.CursorTop = temppos;
                    }
                    
                    if (mergingqueue > 7 + 5*(i/50))
                    {
                        lock(sync)
                        {
                            Console.WriteLine("Waiting for merging..");
                        }
                        while (mergingqueue > 2)
                        {
                            lock(sync)
                            {
                                Console.WriteLine("Queue: {0} tasks left    ", mergingqueue);
                                Console.CursorTop--;
                            }
                            Task.Delay(300).Wait();
                        }
                    }
                }
                stw.Stop();
                lock (sync)
                {
                    Console.WriteLine("FINISHED.\nTotals:\n{0} trigrams, {1} bigrams and {2} 1-grams parsed.\nProcessing time: {3}\nMessages processed: {4}\nMessages skipped: {5}", gen.GetNGramCount(3), gen.GetNGramCount(2), gen.GetNGramCount(1), stw.Elapsed.ToString(), messages, count - messages);
                    Console.WriteLine("Saving to file...");
                    gen.SaveToFile("database_" + item.Title + ".txt");
                    Console.WriteLine("Database for " + item.Title + " was saved.");
                    gen.Clear();
                }
            }
            fs.Flush();
            sw.Close();
            fs.Close();
        }
    }
}
