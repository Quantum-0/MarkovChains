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
}
