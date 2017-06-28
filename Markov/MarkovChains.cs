using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Markov
{
    /*
     * Тссс
     * Оно пока не доделано
     * 👀
     */

    [ProtoContract]
    [ProtoInclude(103, typeof(TriGram))]
    [ProtoInclude(102, typeof(BiGram))]
    [Serializable]
    public abstract class NGram
    {
        [ProtoMember(2)]
        public string Current { get; protected set; }
        [ProtoMember(1)]
        public char Divider { get; protected set; }

        public static NGram Create(IEnumerable<string> words, char divider)
        {
            if (words.Count() == 2)
                return new BiGram(words.ElementAt(0), words.ElementAt(1), divider);
            if (words.Count() == 3)
                return new TriGram(words.ElementAt(0), words.ElementAt(1), words.ElementAt(2), divider);

            return null;
        }

        public abstract bool isNextFor(NGram previous, int checkWords);

        public abstract bool isStart();
        public bool isFull()
        {
            return !(isStart() || isEnd());
        }
        public bool isEnd()
        {
            return Current == null;
        }

        public abstract int Length {get ;}
    }

    [ProtoContract]
    [Serializable]
    internal class BiGram : NGram
    {
        [ProtoMember(3)]
        public string Previous { get; protected set; }

        public override int Length
        {
            get
            {
                var w1 = Previous == null;
                var w2 = Current == null;
                return Convert.ToByte(w1) + Convert.ToByte(w2);
            }
        }

        private BiGram()
        {

        }

        public BiGram(string prev, string cur, char divider)
        {
            if (prev == null && cur == null)
                throw new ArgumentException();

            Previous = prev;
            Current = cur;
            Divider = divider;
        }

        public override bool isNextFor(NGram previous, int checkWords = 1)
        {
            if (checkWords != 1)
                throw new NotImplementedException();

            if (previous is BiGram)
                return this.Previous == (previous as BiGram).Current;
            else if (previous is TriGram)
                return this.Previous == (previous as TriGram).Current;
            else
                throw new NotImplementedException();
        }

        public override bool isStart()
        {
            return Previous == null;
        }
    }

    [ProtoContract]
    [Serializable]
    internal class TriGram : NGram, IEquatable<TriGram>
    {
        [ProtoMember(3)]
        public string Previous { get; protected set; }
        [ProtoMember(4)]
        public string PrePrevious { get; protected set; }

        public override int Length
        {
            get
            {
                var w1 = PrePrevious != null && PrePrevious != "*";
                var w2 = Previous != null;
                var w3 = Current != null;
                return Convert.ToByte(w1) + Convert.ToByte(w2) + Convert.ToByte(w3);
            }
        }

        private TriGram()
        {

        }

        public TriGram(string preprev, string prev, string cur, char divider)
        {
            if ((prev == null && cur == null) || (prev != null) && (preprev == null))
                throw new ArgumentException();

            PrePrevious = preprev;
            Previous = prev;
            Current = cur;
            Divider = divider;
        }

        public override bool isNextFor(NGram previous, int checkWords = 2)
        {
            if (checkWords != 1 && checkWords != 2)
                throw new NotImplementedException();

            if (isStart() || previous.isEnd())
                return false;

            if (previous is BiGram)
                return this.Previous == (previous as BiGram).Current && (checkWords == 1 || this.PrePrevious == (previous as BiGram).Previous);
            else if (previous is TriGram)
                return this.Previous == (previous as TriGram).Current && (checkWords == 1 || this.PrePrevious == (previous as TriGram).Previous);
            else
                throw new NotImplementedException();
        }

        public override bool isStart()
        {
            return Previous == null;
        }

        public override string ToString()
        {
            string _Divider = Divider == ' ' ? " " : (Divider == '-' ? " - " : Divider.ToString() + ' ');

            if (isStart())
                return "START " + Current;
            else if (isEnd())
                return Previous + _Divider + "END";
            else if (PrePrevious == null)
                return "START " + Previous + _Divider + Current;
            else
                return PrePrevious + ' ' + Previous + _Divider + Current;
        }

        public override bool Equals(object obj)
        {
            if (obj is TriGram)
            {
                return Equals((TriGram)obj);
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            /*return (string.IsNullOrEmpty(PrePrevious) || PrePrevious.Length < 2) ? 0 : ((PrePrevious[0] - 32) << 8 + (PrePrevious[1] - 32))
                + (string.IsNullOrEmpty(Previous) ? 0 : (Previous[0] - 32) << 16)
                + (string.IsNullOrEmpty(Current) ? 0 : (Current[0] - 32) << 24);*/
            return PrePrevious?.GetHashCode() ?? 0;
        }

        public bool Equals(TriGram other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Current == (other as TriGram).Current
                    && Previous == (other as TriGram).Previous
                    && PrePrevious == (other as TriGram).PrePrevious;
        }
    }
    
    public class NoStartWordsException : Exception
    {
        public NoStartWordsException() : base("База данных генератора пуста, не удалось сгенерировать предложение")
        {

        }
    }

    /// <summary> Интерфейс генератора текста на основе марковский цепей </summary>
    public interface IMarkovGenerator
    {
        /// <summary> Генерация текста с фиксированным первым словом </summary>
        string GenerateText(string startWord);
        /// <summary> Генерация текста </summary>
        string GenerateText();
        /// <summary> Обучение, на основе текста из потока </summary>
        void LearnText(Stream stream, Encoding encoding = null);
        /// <summary> Обучение, на основе текста </summary>
        void LearnText(string text);
    }

    /// <summary> Интерфейс объекта, способного сериализоваться и десериализоваться из файла </summary>
    public interface IFileSerializable
    {
        /// <summary> Сохранение в файл </summary> <param name="filename">Путь к файлу</param>
        void SaveToFile(string filename);
        /// <summary> Загрузка из файла </summary> <param name="filename">Путь к файлу</param>
        void LoadFromFile(string filename);
    }

    /// <summary> Расширенный интерфейс генератора текста на основе марковский цепей </summary>
    public interface IExtendedMarkovGenerator : IMarkovGenerator, IFileSerializable
    {
        int GetNGramCount(int n);
        int GetNGramCount();
        int GetStartNGramCount();
        int GetEndNGramCount();
        IEnumerable<string> GetStartWords();
        IEnumerable<string> GetEndWords();
        void Union(IExtendedMarkovGenerator other);
        void Clear();
    }

    /// <summary> Базовый абстрактный генератор текста на основе марковский цепей </summary>
    public abstract class MarkovGenerator : IExtendedMarkovGenerator
    {
        //123
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
                //return (T)binaryFormatter.Deserialize(stream);
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
                dividers = new char[]{ '!', '?', '.'};

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

    /// <summary> Генератор текстов на основе марковских цепей из пар слов </summary>
    public class BiMarkovGenerator : MarkovGenerator
    {
        /// <summary> Пары слов </summary>
        private HashSet<BiGram> Pairs;
        
        public BiMarkovGenerator()
        {
            Pairs = new HashSet<BiGram>();
        }

        public override int GetNGramCount(int n)
        {
            if (n == 2)
                return Pairs.Count(p => p.isFull());
            else if (n == 1)
                return Pairs.Count(p => p.isStart() || p.isEnd());
            else
                return 0;
        }

        public string[] GetWords(string wordBefore)
        {
            return Pairs.Where(w => w.Previous == wordBefore.ToLower()).Select(w => w.Current).ToArray();
        }

        private BiGram GetNextRandom(BiGram before)
        {
            if (before.isEnd())
                return null;

            return Pairs.Where(w => w.isNextFor(before)).OrderBy(w => Rnd.Next()).FirstOrDefault();
        }

        public string GetRandomWord(string wordBefore)
        {
            return GetWords(wordBefore).OrderBy(w => Rnd.Next()).FirstOrDefault();
        }

        public override string GenerateText(string startWord)
        {
            throw new NotImplementedException();
        }
        
        /*private string ContinueGeneratingText(NGram curword)
        {
            curword
        }*/

        public override string GenerateText()
        {
            //LinkedList<string> Text = new LinkedList<string>();
            StringBuilder sb = new StringBuilder();
            var curword = Pairs.Where(w => w.isStart()).OrderBy(w => Rnd.Next()).FirstOrDefault();
            if (curword == null)
                throw new NoStartWordsException();

            sb.Append(curword.Current.StartWithUpper());
            while (curword.Current != null)
            {
                curword = GetNextRandom(curword);
                //if (curword.Divider != ' ')
                //Text.AddLast(curword.Divider.ToString());
                if (curword.Current != null)
                switch(curword.Divider)
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

        protected override void Learn(List<string> sentenses)
        {
            foreach (var sentense in sentenses)
            {
                var parsed = ParseSentence(sentense);

                var words = parsed.Item1;
                var dividers = parsed.Item2;

                if (words.Count < 3)
                    continue;

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
        }

        protected override void AddStart(params string[] words)
        {
            Pairs.Add(new BiGram(null, words[0], ' '));
        }

        protected override void AddEnd(char divider, params string[] words)
        {
            Pairs.Add(new BiGram(words[0], null, divider));
        }

        protected override void Add(char divider, params string[] words)
        {
            Pairs.Add(new BiGram(words[0], words[1], divider));
        }

        protected override IEnumerable<NGram> GetStartNGrams()
        {
            return Pairs.Where(p => p.isStart());
        }

        protected override IEnumerable<NGram> GetEndNGrams()
        {
            return Pairs.Where(p => p.isEnd());
        }

        public override void SaveToFile(string filename)
        {
            _SaveToFile<HashSet<BiGram>>(filename, Pairs);
        }

        public override void LoadFromFile(string filename)
        {
            Pairs = _LoadFromFile<HashSet<BiGram>>(filename);
        }

        public override void Clear()
        {
            Pairs.Clear();
        }

        public override void Union(IExtendedMarkovGenerator other)
        {
            Pairs.UnionWith(((BiMarkovGenerator)other).Pairs);
        }

        public override int GetNGramCount()
        {
            return Pairs.Count;
        }
    }
    
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














internal static class StringAndArrayExtensions
{
    public static string StartWithUpper(this string Str)
    {
        return Str.Substring(0, 1).ToUpperInvariant() + Str.Substring(1).ToLowerInvariant();
    }

   /* public static NGram GetRandom(this NGram[] Str)
    {
        return Str.Substring(0, 1).ToUpperInvariant() + Str.Substring(1).ToLowerInvariant();
    }*/
}
}