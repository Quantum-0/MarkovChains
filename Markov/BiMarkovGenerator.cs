using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Markov
{
    /// <summary> Генератор текстов на основе марковских цепей из пар слов </summary>
    public class BiMarkovGenerator : MarkovGenerator
    {
        /// <summary> Минимальная длина предложения </summary>
        protected override int MinSentenseLength { get; } = 3;

        /// <summary> Обучение на списке предложений </summary>
        protected override void Learn(List<string> words, List<char> dividers)
        {
            AddStart(words[0]);
            if (dividers.Count == words.Count)
                AddEnd(dividers.Last(), words.Last());
            else
                AddEnd(words.Last());

            for (int i = 0; i < words.Count - 1; i++)
            {
                var first = words[i];
                var second = words[i + 1];
                var divider = dividers[i];
                Add(divider, first, second);
            }
        }

        /// <summary> Продолжение генерации текста после n-граммы <paramref name="start"/> </summary> <param name="start">Начальная N-грамма</param>
        protected override string ContinueGeneratingText(NGram start)
        {
            var sb = new StringBuilder();
            var curword = start;

            sb.Append(curword.Current.StartWithUpper());
            while (curword.Current != null)
            {
                curword = Ngrams.Where(w => w.isNextFor(curword, 1)).OrderBy(w => Rnd.Next()).FirstOrDefault();
                
                if (curword.Current != null)
                    switch (curword.Divider)
                    {
                        case '-':
                            sb.Append(" - " + curword.Current);
                            break;
                        case ',':
                            sb.Append(", " + curword.Current);
                            break;
                        case '.':
                            sb.Append(". " + curword.Current.StartWithUpper());
                            break;
                        case ';':
                            sb.Append("; " + curword.Current);
                            break;
                        case ' ':
                            sb.Append(' ' + curword.Current);
                            break;
                        case '\n':
                            sb.Append(". " + curword.Current.StartWithUpper());
                            break;
                        default:
                            sb.Append(curword.Divider + curword.Current);
                            break;
                    }
            }

            if (curword.Divider != ' ')
                sb.Append(curword.Divider);

            return sb.ToString();
        }
        
        /// <summary> Добавление N-граммы, являющейся началом предложения </summary>
        protected override void AddStart(params string[] words)
        {
            if (words.Length != 1)
                throw new ArgumentException();
            Add(words);
        }

        /// <summary> Добавление N-граммы, являющейся концом предложения </summary>
        protected override void AddEnd(char divider, params string[] words)
        {
            if (words.Length != 1)
                throw new ArgumentException();
            Add(divider, words[0], null);
        }
        
        /// <summary> Добавление N-граммы </summary> <param name="divider">Символ - разделитель</param> <param name="words">Слова</param>
        protected override void Add(char divider, params string[] words)
        {
            BiGram New;

            if (words.Length == 2)
                New = new BiGram(words[0], words[1], divider);
            else if (words.Length == 1)
                New = new BiGram(null, words[0], divider);
            else
                throw new ArgumentException();

            Ngrams.Add(New);
        }

        /// <summary> Сохранение в файл </summary> <param name="filename">Путь к файлу</param>
        public override void SaveToFile(string filename)
        {
            _SaveToFile<BiGram[]>(filename, Ngrams.Cast<BiGram>().ToArray());
        }

        /// <summary> Загрузка из файла </summary> <param name="filename">Путь к файлу</param>
        public override void LoadFromFile(string filename)
        {
            Ngrams = new HashSet<NGram>(_LoadFromFile<BiGram[]>(filename));
        }
    }
}
