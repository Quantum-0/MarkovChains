using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Markov
{
    public static class UpdateOldDatabase
    {
        public static void UpdateTrigramDB(string fname, string newfname)
        {
            HashSet<TriGram> Old;
            using (Stream stream = File.Open(fname, FileMode.Open))
            {
                Old = Serializer.Deserialize<HashSet<TriGram>>(stream);
            }

            TriGram[] New = Old.Where(t => t.PrePrevious != "*").ToArray();

            using (Stream stream = File.Open(newfname, FileMode.Create))
            {
                Serializer.Serialize<TriGram[]>(stream, New);
            }

            //test

            var test = new TrigramMarkovGenerator();
            test.LoadFromFile(newfname);
            ;
        }
    }

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
    public class BiGram : NGram
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
    public class TriGram : NGram, IEquatable<TriGram>
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
            if ((prev == null && cur == null) || (cur == null && prev != null && preprev == null))
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
                return PrePrevious + ' ' + Previous + _Divider + "END";
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
    
    internal static class StringAndArrayExtensions
    {
        public static string StartWithUpper(this string Str)
        {
            return Str.Substring(0, 1).ToUpperInvariant() + Str.Substring(1).ToLowerInvariant();
        }
    }  
}