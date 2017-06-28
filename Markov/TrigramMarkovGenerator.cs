using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markov
{
    public class TrigramMarkovGenerator : MarkovGenerator
    {
        /// <summary> Минимальная длина предложения </summary>
        protected override int MinSentenseLength { get; } = 4;

        /// <summary> Обучение на списке предложений </summary>
        protected override void Learn(List<string> words, List<char> dividers)
        {
            AddStart(words[0]);
            AddStart(words[0], words[1]);
            if (dividers.Count == words.Count)
                AddEnd(dividers.Last(), words[words.Count - 2], words.Last());
            else
                AddEnd(words[words.Count - 2], words.Last());

            // Bigrams
            /*for (int i = 0; i < words.Count - 1; i++)
            {
                var first = words[i];
                var second = words[i + 1];
                var divider = dividers[i];
                Add(divider, "*", first, second);
            }*/

            // Trigrams
            for (int i = 0; i < words.Count - 2; i++)
            {
                var first = words[i];
                var second = words[i + 1];
                var third = words[i + 2];
                var divider = dividers[i + 1];
                Add(divider, first, second, third);
            }

        }

        /// <summary> Продолжение генерации текста после n-граммы <paramref name="start"/> </summary> <param name="start">Начальная N-грамма</param>
        protected override string ContinueGeneratingText(NGram start)
        {
            var sb = new StringBuilder();
            var curword = start;

            while (!curword.isEnd())
            {
                //Вставляем слово
                if (curword.isStart())
                    sb.Append(curword.Current.StartWithUpper());
                else
                    sb.Append(curword.Current);

                // Берём следующее слово
                curword = GetNext(curword);

                // Вставляем разделитель
                if (curword.Divider == '-')
                    sb.Append(' ');
                if (curword.Divider != ' ')
                    sb.Append(curword.Divider.ToString() + ' ');
                else if (!curword.isEnd())
                    sb.Append(' ');
            }

            return sb.ToString();
        }

        /// <summary> Добавление N-граммы, являющейся началом предложения </summary>
        protected override void AddStart(params string[] words)
        {
            if (words.Length < 1 || words.Length > 2)
                throw new ArgumentException();
            Add(words);
        }

        /// <summary> Добавление N-граммы, являющейся концом предложения </summary>
        protected override void AddEnd(char divider, params string[] words)
        {
            if (words.Length != 2)
                throw new ArgumentException();
            Add(divider, words[0], words[1], null);
        }

        /// <summary> Добавление N-граммы </summary> <param name="divider">Символ - разделитель</param> <param name="words">Слова</param>
        protected override void Add(char divider = ' ', params string[] words)
        {
            TriGram New;

            if (words.Length == 3)
                New = new TriGram(words[0], words[1], words[2], divider);
            else if (words.Length == 2)
                New = new TriGram(null, words[0], words[1], divider);
            else if (words.Length == 1)
                New = new TriGram(null, null, words[0], divider);
            else
                throw new ArgumentException();

            Ngrams.Add(New);
        }

        /// <summary> Сохранение в файл </summary> <param name="filename">Путь к файлу</param>
        public override void SaveToFile(string filename)
        {
            _SaveToFile<TriGram[]>(filename, Ngrams.Cast<TriGram>().ToArray());
        }

        /// <summary> Загрузка из файла </summary> <param name="filename">Путь к файлу</param>
        public override void LoadFromFile(string filename)
        {
            Ngrams = new HashSet<NGram>(_LoadFromFile<TriGram[]>(filename));
        }



        /// <summary>
        /// Получение случайной следующей триграммы за текущей
        /// </summary>
        /// <param name="previous">Текущая триграмма, за которой будет получена следующая</param>
        /// <param name="biIfMoreThanOneTri">Шанс генерации биграммы вместо триграммы, если найдено более 1 триграммы, следующей за текущей</param>
        /// <param name="biIfOnlyOneTri">Шанс генерации биграммы вместо триграммы, если найдена лишь одна подходящая триграмма</param>
        /// <returns></returns>
        private NGram GetNext(NGram previous, double biIfMoreThanOneTri = 0.2, double biIfOnlyOneTri = 0.85)
        {
            var bi = Ngrams.Where(t => t.isNextFor(previous, 1)).ToArray();
            var tri = bi.Where(t => t.isNextFor(previous, 2)).ToArray();

            var count3 = tri.Length;
            var count2 = bi.Length;

            if (count2 == 0)
                return null;

            if (count3 == 1)
                if (Rnd.NextDouble() > biIfOnlyOneTri)
                    return tri[0];

            if (count3 > 1)
                if (Rnd.NextDouble() > biIfMoreThanOneTri)
                    return tri[Rnd.Next(tri.Length)];

            return bi[Rnd.Next(bi.Length)];
        }
    }
}
