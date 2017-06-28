namespace Markov
{
    /// <summary> Интерфейс генератора текста на основе марковский цепей </summary>
    public interface IMarkovGenerator
    {
        /// <summary> Генерация текста с фиксированным первым словом </summary>
        string GenerateText(string startWord);
        /// <summary> Генерация текста </summary>
        string GenerateText();
        /// <summary> Обучение, на основе текста из потока </summary>
        void LearnText(System.IO.Stream stream, System.Text.Encoding encoding = null);
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
        System.Collections.Generic.IEnumerable<string> GetStartWords();
        System.Collections.Generic.IEnumerable<string> GetEndWords();
        void Union(IExtendedMarkovGenerator other);
        void Clear();
    }
}