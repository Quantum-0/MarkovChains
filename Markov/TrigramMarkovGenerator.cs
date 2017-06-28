using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markov
{
    public class TrigramMarkovGenerator : MarkovGenerator
    {
        /// <summary> Триграммы слов с разделителями </summary>
        private HashSet<TriGram> Trigrams = new HashSet<TriGram>();
        /// <summary> Минимальная длина предложения </summary>
        private const int MinSentenseLength = 4;
        /// <summary> Разрешены ли повторения триграмм </summary>
        //private bool AllowDuplicates = true;

        /// <summary> Обучение на списке предложений </summary>
        protected override void Learn(List<string> sentenses)
        {
            foreach (var sentense in sentenses)
            {
                var parsed = ParseSentence(sentense);

                var words = parsed.Item1;
                var dividers = parsed.Item2;

                if (words.Count < MinSentenseLength)
                    continue;

                AddStart(words[0]);
                if (dividers.Count == words.Count)
                    AddEnd(dividers.Last(), words[words.Count - 2], words.Last());
                else
                    AddEnd(words[words.Count - 2], words.Last());

                // Bigrams
                for (int i = 0; i < words.Count - 1; i++)
                {
                    var first = words[i];
                    var second = words[i + 1];
                    var divider = dividers[i];
                    Add(divider, "*", first, second);
                }

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

            Trigrams.Add(New);
        }

        /// <summary> Продолжение генерации текста после n-граммы <paramref name="start"/> </summary> <param name="start">Начальная N-грамма</param>
        private string ContinueGeneratingText(TriGram start)
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

        /// <summary> Генерация текста, начинающегося с заданного слова </summary> <param name="startWord">Слово, с которого должно начинаться сгенерированное предложение</param>
        public override string GenerateText(string startWord)
        {
            startWord = startWord.ToLower();
            var curword = Trigrams.Where(w => w.isStart() && w.Current.ToLower() == startWord).OrderBy(w => Rnd.Next()).FirstOrDefault();
            if (curword == null)
                return "";

            return ContinueGeneratingText(curword);
        }

        /// <summary> Генерация текста </summary>
        public override string GenerateText()
        {
            var curword = Trigrams.Where(w => w.isStart()).OrderBy(w => Rnd.Next()).FirstOrDefault();
            if (curword == null)
                throw new NoStartWordsException();

            return ContinueGeneratingText(curword);
        }

        /// <summary>
        /// Получение случайной следующей триграммы за текущей
        /// </summary>
        /// <param name="previous">Текущая триграмма, за которой будет получена следующая</param>
        /// <param name="biIfMoreThanOneTri">Шанс генерации биграммы вместо триграммы, если найдено более 1 триграммы, следующей за текущей</param>
        /// <param name="biIfOnlyOneTri">Шанс генерации биграммы вместо триграммы, если найдена лишь одна подходящая триграмма</param>
        /// <returns></returns>
        private TriGram GetNext(TriGram previous, double biIfMoreThanOneTri = 0.2, double biIfOnlyOneTri = 0.85)
        {
            var bi = Trigrams.Where(t => t.isNextFor(previous, 1)).ToArray();
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

        /// <summary> Получение количества N-грамм </summary>
        public override int GetNGramCount(int n)
        {
            lock (Sync)
            {
                if (n < 1 || n > 3)
                    return 0;

                return Trigrams.Count(t => t.Length == n);
            }
        }

        /// <summary> Получение количества N-грамм </summary>
        public override int GetNGramCount()
        {
            lock (Sync)
            {
                return Trigrams.Count;
            }
        }

        /// <summary> Добавление N-граммы, являющейся началом предложения </summary>
        protected override void AddStart(params string[] words)
        {
            Add(words);
        }

        /// <summary> Добавление N-граммы, являющейся концом предложения </summary>
        protected override void AddEnd(char divider, params string[] words)
        {
            if (words.Length != 2)
                throw new ArgumentException();
            Add(divider, words[0], words[1], null);
        }

        protected override IEnumerable<NGram> GetStartNGrams()
        {
            return Trigrams.Where(t => t.isStart());
        }

        protected override IEnumerable<NGram> GetEndNGrams()
        {
            return Trigrams.Where(t => t.isEnd());
        }

        public override void SaveToFile(string filename)
        {
            _SaveToFile<HashSet<TriGram>>(filename, Trigrams);
        }

        public override void LoadFromFile(string filename)
        {
            Trigrams = _LoadFromFile<HashSet<TriGram>>(filename);
        }

        public override void Clear()
        {
            Trigrams.Clear();
        }

        public override void Union(IExtendedMarkovGenerator other)
        {
            lock (Sync)
            {
                Trigrams.UnionWith((other as TrigramMarkovGenerator).Trigrams);
            }
        }
    }
}
