using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markov;
using System.IO;
using System.Diagnostics;

namespace MarkovTest
{
    class Program
    {
        static string teststr1 = "The quick brown fox jumps over the lazy dog! В чащах юга жил бы цитрус? Да, но фальшивый экземпляр!";
        static string teststr2 = "Любя, съешь щипцы, — вздохнёт мэр, — кайф жгуч. Эх, чужак! Общий съём цен шляп (юфть) — вдрызг! Эх, чужд кайф, сплющь объём вши, грызя цент.";
        
        static void UnionAndSerializationExample()
        {
            Console.WriteLine("union and serialization test");

            IExtendedMarkovGenerator gen1 = new BiMarkovGenerator();
            IExtendedMarkovGenerator gen2 = new BiMarkovGenerator();

            Parallel.For(0, 2, (i) =>
            {
                if (i == 0)
                {
                    Console.WriteLine("gen1 started learning");
                    Task.Delay(5).Wait();
                    gen1.LearnText(teststr1);
                    Console.WriteLine("gen1 finished learning");
                    Console.WriteLine(gen1.GetNGramCount() + " learned");
                }
                else
                {
                    Console.WriteLine("gen2 started learning");
                    Task.Delay(5).Wait();
                    gen2.LearnText(teststr2);
                    Console.WriteLine("gen2 finished learning");
                    Console.WriteLine(gen2.GetNGramCount() + " learned");
                }
            });

            Console.WriteLine("Merging");
            gen1.Union(gen2);
            gen2.Clear();

            Console.WriteLine(gen1.GetNGramCount() + " - total count (gen1)");
            Console.WriteLine(gen2.GetNGramCount() + " - gen2");
            gen1.SaveToFile("test1.txt");
            Console.WriteLine("gen1 saved to file test1.txt");
            gen2.LoadFromFile("test1.txt");
            Console.WriteLine("gen2 loaded from file test1.txt");
            gen1.Clear();

            Console.WriteLine(gen1.GetNGramCount() + " - cleared gen1");
            Console.WriteLine(gen2.GetNGramCount() + " - loaded gen2");
        }

        static void Main(string[] args)
        {

             UnionAndSerializationExample();


            // Создаём генератор текста
            IExtendedMarkovGenerator generator = new BiMarkovGenerator();
            
            // Включаем замер памяти и времени
            GC.Collect();
            var before = GC.GetTotalMemory(false);
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Учимся
            //generator.LearnText(teststr1);
            //generator.LearnText(teststr2);
            generator.LearnText("abc abd bcd bcd abc abd? abc add abb abd bcd abd acc abc!");

            // Загрузка данных из базы данных
            /*var fname = "database.db";
            generator.LoadFromFile(fname);*/

            // Учимся по тексту из файла
            /*var fname = @"messages_dump.txt";
            using (var fs = new FileStream(fname, FileMode.Open, FileAccess.Read, FileShare.Read))
                generator.LearnText(fs);*/

            // Останавливаем замер памяти и времени
            sw.Stop();
            var after = GC.GetTotalMemory(false);

            // Выводим полученную информацию
            Console.WriteLine("Затрачено времени: " + sw.ElapsedMilliseconds + "мс");
            Console.WriteLine("Добавлено единичных слов: " + generator.GetNGramCount(1));
            Console.WriteLine("Добавлено пар слов: " + generator.GetNGramCount(2));
            Console.WriteLine("Добавлено триграмм: " + generator.GetNGramCount(3));
            Console.WriteLine("Количество возможных начал предложения: " + generator.GetStartNGramCount());
            Console.WriteLine("Количество возможных концов предложения: " + generator.GetEndNGramCount());
            Console.WriteLine("Размер занятой памяти увеличился на: " + ((after - before) / 1024f) + " кб");
            GC.Collect(2, GCCollectionMode.Forced, true);
            after = GC.GetTotalMemory(false);
            Console.WriteLine("После очистки мусора: " + ((after - before) / 1024f) + " кб\n\n");
            
            // Генерируем и выводим предложения
            for (int i = 0; i < 50; i++)
            {
                var text = string.Join(" ", generator.GenerateText());
                Console.WriteLine(text);
            }

            // Ждём нажатия Enter для закрытия
            Console.ReadLine();
        }
    }
}
