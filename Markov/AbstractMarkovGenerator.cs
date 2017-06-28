using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Markov
{
    /// <summary> Базовый абстрактный генератор текста на основе марковский цепей </summary>
    public abstract class MarkovGenerator : IExtendedMarkovGenerator
    {
        /// <summary> База данных N-грам </summary>
        protected internal HashSet<NGram> Ngrams = new HashSet<NGram>();
        /// <summary> Используемый для случайностей генератор случайных чисел </summary>
        protected Random Rnd = new Random();
        /// <summary> Массив запрещённых символов </summary>
        private char[] DeniedDividers = new char[] { '\"', '\'', '\\', '/', '<', '>', '(', ')', '[', ']' };
        /// <summary> Массив разделителей предложений </summary>
        private char[] DefaultDividers = new char[] { '!', '?', '.' };

        protected object Sync = new object();

        protected void _SaveToFile<T>(string fname, T obj)
        {
            lock (Sync)
            {
                using (Stream stream = File.Open(fname, FileMode.Create))
                {
                    var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    //binaryFormatter.Serialize(stream, obj);
                    Serializer.Serialize<T>(stream, obj);
                }
            }
        }

        protected T _LoadFromFile<T>(string fname)
        {
            using (Stream stream = File.Open(fname, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return Serializer.Deserialize<T>(stream);
            }
        }

        public abstract void SaveToFile(string filename);
        public abstract void LoadFromFile(string filename);

        /// <summary> Получение количества N-грамм в базе данных </summary> <param name="n">количество слов N-граммы</param> <returns>количество N-грамм</returns>
        public abstract int GetNGramCount(int n);

        /// <summary> Получение количества начал предложения </summary> <returns>количество N-грамм</returns>
        public int GetStartNGramCount()
        {
            return GetStartNGrams().Count();
        }

        /// <summary> Получение всех возможных начал предложения </summary> <returns>N-граммы</returns>
        protected abstract IEnumerable<NGram> GetStartNGrams();

        /// <summary> Получение всех возможных слов, с которых может начинаться предложение </summary>
        public IEnumerable<string> GetStartWords()
        {
            return GetStartNGrams().Select(n => n.Current);
        }

        /// <summary> Получение количества концов предложения </summary> <returns>количество N-грамм</returns>
        public int GetEndNGramCount()
        {
            return GetEndNGrams().Count();
        }

        /// <summary> Получение всех возможных начал предложения </summary> <returns>N-граммы</returns>
        protected abstract IEnumerable<NGram> GetEndNGrams();

        /// <summary> Получение всех возможных слов, на которые может оканчиваться предложение </summary>
        public IEnumerable<string> GetEndWords()
        {
            return GetEndNGrams().Select(n => n is BiGram ? (n as BiGram).Previous : (n as TriGram).Previous);
        }

        /// <summary> Разбиение предложения на список слов и разделителей </summary> <param name="Sentence">Предложение</param> <returns>Список слов и разделителей</returns>
        protected Tuple<List<string>, List<char>> ParseSentence(string sentence)
        {
            var wordsWithDividers = Regex.Matches(sentence, @"(\w+)(\W*)");
            var words = new List<string>();
            var _dividers = new List<string>();
            var dividers = new List<char>(_dividers.Count);
            foreach (Match subres in wordsWithDividers)
            {
                words.Add(subres.Groups[1].Value);
                _dividers.Add(subres.Groups[2].Value);
            }

            for (int i = 0; i < _dividers.Count; i++)
            {
                var temp = _dividers[i].ToList();
                temp.RemoveAll(c => DeniedDividers.Contains(c));
                _dividers[i] = string.Join("", temp);
            }

            foreach (var div in _dividers)
            {
                if (div.Length == 0 || div.All(c => c == ' ')) // "" or "     "
                    dividers.Add(' ');
                else if (div.Length == 1 || div.All(c => c == div[0])) // "?" or "?????"
                    dividers.Add(div[0]);
                else
                {
                    // "? ? ??? !" -> "?????!"
                    var temp = div.ToList();
                    temp.RemoveAll(c => c == ' ');
                    var newdiv = string.Join("", temp);

                    if (newdiv.Length == 1)
                        dividers.Add(newdiv[0]);
                    else
                    {
                        if (newdiv.Contains("?"))
                            dividers.Add('?');
                        else if (newdiv.Contains("!"))
                            dividers.Add('!');
                        else if (newdiv.Contains("-"))
                            dividers.Add('-');
                        else if (newdiv.Contains("."))
                            dividers.Add('.');
                        else
                            dividers.Add(' ');
                    }
                }
            }

            return new Tuple<List<string>, List<char>>(words, dividers);
        }

        /// <summary> Разбитие текста на предложения </summary> <param name="text">Текст</param> <param name="dividers">Разделители</param> <returns>Список предложений</returns>
        protected List<string> SplitText(string text, char[] dividers = null)
        {
            if (dividers == null)
                dividers = DefaultDividers;

            List<string> Sentenses = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool end = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (dividers.Contains(text[i]))
                {
                    end = true;
                    sb.Append(text[i]);
                }
                else
                {
                    if (!end)
                        sb.Append(text[i]);
                    else
                    {
                        if (sb.Length > 0)
                            Sentenses.Add(sb.ToString());
                        sb.Clear();
                        end = false;
                    }
                }
            }
            if (sb.Length > 0)
                Sentenses.Add(sb.ToString());

            return Sentenses;
        }

        /// <summary> Разбитие текста из потока на предложения </summary> <param name="stream">Поток, который нужно считать</param>
        /// <param name="encoding">Кодировка символов, которую нужно использовать</param> <param name="dividers">Разделители</param> <returns>Список предложений</returns>
        protected List<string> SplitText(Stream stream, Encoding encoding = null, char[] dividers = null)
        {
            if (dividers == null)
                dividers = new char[] { '!', '?', '.' };

            List<string> sentenses = new List<string>();
            using (StreamReader sr = encoding == null ? new StreamReader(stream) : new StreamReader(stream, encoding))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    sentenses.AddRange(SplitText(line, dividers));
                }
            }
            return sentenses;
        }

        /// <summary> Генерация текста </summary> <returns>Сгенерированный текст</returns>
        public abstract string GenerateText();

        /// <summary> Генерация текста с фиксированным началом </summary>
        public abstract string GenerateText(string startWord);

        /// <summary> Добавление текста в бд </summary>
        public void LearnText(string text)
        {
            var splitted = SplitText(text);
            Learn(splitted);
        }

        /// <summary> Добавление текста из потока в бд </summary>
        public void LearnText(Stream stream, Encoding encoding = null)
        {
            var splitted = SplitText(stream, encoding);
            Learn(splitted);
        }

        /// <summary> Обучение на списке предложений </summary>
        protected abstract void Learn(List<string> sentenses);

        /// <summary> Добавление N-граммы, являющейся началом предложения </summary>
        protected abstract void AddStart(params string[] words);
        /// <summary> Добавление N-граммы, являющейся концом предложения </summary>
        protected void AddEnd(params string[] words)
        {
            AddEnd(' ', words);
        }
        /// <summary> Добавление N-граммы со знаком, являющейся концом предложения </summary>
        protected abstract void AddEnd(char divider, params string[] words);
        /// <summary> Добавление N-граммы </summary>
        protected void Add(params string[] words)
        {
            Add(' ', words);
        }
        /// <summary> Добавление N-граммы со знаком </summary>
        protected abstract void Add(char divider, params string[] words);

        public abstract void Union(IExtendedMarkovGenerator other);

        public abstract void Clear();
        public abstract int GetNGramCount();
    }
}
