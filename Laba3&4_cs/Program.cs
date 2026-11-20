using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Laba3And4_cs
{

    public abstract class Token
    {
        [XmlText]
        public string Value { get; set; } = string.Empty; 

        protected Token() { }
        protected Token(string value) => Value = value;

        public override string ToString() => Value;
    }

    public class Word : Token
    {
        public Word() : base() { }
        public Word(string value) : base(value) { }
    }

    public class Punctuation : Token
    {
        public Punctuation() : base() { }
        public Punctuation(string value) : base(value) { }
    }

    public class Sentence
    {
        [XmlElement("word", Type = typeof(Word))]
        [XmlElement("punctuation", Type = typeof(Punctuation))]
        public List<Token> Tokens { get; set; } = new List<Token>();

        [XmlIgnore]
        public IEnumerable<Word> Words => Tokens.OfType<Word>();

        [XmlIgnore]
        public bool IsQuestion => Tokens.LastOrDefault()?.Value == "?";

        [XmlIgnore]
        public int Length => ToString().Length;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Tokens.Count; i++)
            {
                sb.Append(Tokens[i].Value);
                if (i < Tokens.Count - 1 && Tokens[i] is Word && Tokens[i + 1] is Word)
                {
                    sb.Append(" ");
                }
                else if (i < Tokens.Count - 1 && Tokens[i] is Word && Tokens[i + 1].Value == "-")
                {
                    sb.Append(" ");
                }
                else if (i < Tokens.Count - 1 && Tokens[i].Value == "-" && Tokens[i + 1] is Word)
                {
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }
    }

    [XmlRoot("text")]
    public class Text
    {
        [XmlElement("sentence")]
        public List<Sentence> Sentences { get; set; } = new List<Sentence>();

        [XmlIgnore]
        public string[] RawLines { get; set; } = Array.Empty<string>();

        public override string ToString()
        {
            return string.Join("  ", Sentences.Select(s => s.ToString()));
        }

        public Dictionary<string, WordEntry> BuildConcordance()
        {
            var concordance = new Dictionary<string, WordEntry>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < RawLines.Length; i++)
            {
                int lineNumber = i + 1;
                string currentLine = RawLines[i];

                var words = Regex.Matches(currentLine, @"[a-zA-Zа-яА-Я0-9']+")
                                 .Cast<Match>()
                                 .Select(m => m.Value.ToLower())
                                 .Where(w => !string.IsNullOrWhiteSpace(w));

                foreach (string word in words)
                {
                    if (!concordance.ContainsKey(word))
                    {
                        concordance[word] = new WordEntry();
                    }

                    concordance[word].Frequency++;
                    concordance[word].Lines.Add(lineNumber);
                }
            }

            return concordance;
        }

        public void PrintConcordance(Dictionary<string, WordEntry> concordance)
        {
            var sortedConcordance = concordance.OrderBy(kvp => kvp.Key);

            Console.WriteLine("ЛАБОРАТОРНАЯ №4:");
            foreach (var entry in sortedConcordance)
            {
                string word = entry.Key;
                int freq = entry.Value.Frequency;
                string linesList = string.Join(" ", entry.Value.Lines);

                string wordWithDots = word.PadRight(25, '.');
                Console.WriteLine($"{wordWithDots}{freq}: {linesList}");
            }
        }
    }

    public class WordEntry
    {
        public int Frequency { get; set; }
        public SortedSet<int> Lines { get; set; } = new SortedSet<int>();
    }

    public static class TextParser
    {
        public static Text Parse(string[] rawLines)
        {
            string rawText = string.Join(" ", rawLines);
            Text textObj = new Text { RawLines = rawLines };

            string sentenceReg = @"([^.!?]+[.!?]*)";
            var sentenceMatches = Regex.Matches(rawText, sentenceReg);

            foreach (Match sMatch in sentenceMatches)
            {
                string sText = sMatch.Value.Trim();
                if (string.IsNullOrWhiteSpace(sText)) continue;

                Sentence sentence = new Sentence();
                string tokenReg = @"([a-zA-Zа-яА-Я0-9']+)|([^a-zA-Zа-яА-Я0-9'\s]+)";
                var tokenMatches = Regex.Matches(sText, tokenReg);

                foreach (Match tMatch in tokenMatches)
                {
                    string val = tMatch.Value;
                    if (Regex.IsMatch(val, @"^[a-zA-Zа-яА-Я0-9']+$"))
                    {
                        sentence.Tokens.Add(new Word(val));
                    }
                    else
                    {
                        sentence.Tokens.Add(new Punctuation(val));
                    }
                }

                if (sentence.Tokens.Count > 0)
                {
                    textObj.Sentences.Add(sentence);
                }
            }

            return textObj;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

           
            string inputFile = "input.txt";
            if (!File.Exists(inputFile))
            {
                string[] defaultText = new string[]
                {
                    "Peter Piper picked a peck of pickled peppers. A peck of pickled",
                    "peppers Peter Piper picked. If Peter Piper picked a peck of",
                    "pickled peppers, where is the peck that Peter Piper picked?"
                };
                File.WriteAllLines(inputFile, defaultText, Encoding.UTF8);
            }

            string[] rawLines = File.ReadAllLines(inputFile, Encoding.UTF8);

            Console.WriteLine("ИСХОДНЫЙ ТЕКСТ ИЗ ФАЙЛА input.txt:");
            foreach (var line in rawLines) Console.WriteLine(line);
            Console.WriteLine(new string('=', 50) + "\n");

            
            Text myText = TextParser.Parse(rawLines);

            
            var concordanceResult = myText.BuildConcordance();
            myText.PrintConcordance(concordanceResult);
            Console.WriteLine("\n" + new string('=', 50) + "\n");

            
            Console.WriteLine("РЕЗУЛЬТАТЫ ЛАБОРАТОРНОЙ №3:");

            
            Console.WriteLine("\n1. Предложения по возрастанию количества слов:");
            foreach (var s in myText.Sentences.OrderBy(s => s.Words.Count()))
            {
                Console.WriteLine($"[{s.Words.Count()} слов]: {s}");
            }

            
            Console.WriteLine("\n2. Предложения по возрастанию длины в символах:");
            foreach (var s in myText.Sentences.OrderBy(s => s.Length))
            {
                Console.WriteLine($"[{s.Length} симв]: {s}");
            }

            
            int targetLen3 = 4;
            Console.WriteLine($"\n3. Уникальные слова длины {targetLen3} в вопросительных предложениях:");
            var questionWords = myText.Sentences
                .Where(s => s.IsQuestion)
                .SelectMany(s => s.Words)
                .Select(w => w.Value.ToLower())
                .Distinct()
                .Where(w => w.Length == targetLen3);
            Console.WriteLine(string.Join(", ", questionWords));

           
            int targetLen4 = 4;
            string consonants = "bcdfghjklmnpqrstvwxyzбвгджзйклмнпрстфхцчшщ";
            foreach (var s in myText.Sentences)
            {
                s.Tokens.RemoveAll(t => t is Word &&
                                        t.Value.Length == targetLen4 &&
                                        consonants.Contains(char.ToLower(t.Value[0])));
            }
            Console.WriteLine($"\n4. Текст после удаления слов длины {targetLen4} на согласную:\n{myText}");

         
            int targetLen5 = 2;
            string substring = "[SUBST]";
            if (myText.Sentences.Count > 0)
            {
                var firstTokens = myText.Sentences[0].Tokens;
                for (int i = 0; i < firstTokens.Count; i++)
                {
                    if (firstTokens[i] is Word && firstTokens[i].Value.Length == targetLen5)
                    {
                        firstTokens[i] = new Word(substring);
                    }
                }
            }
            Console.WriteLine($"\n5. Первое предложение после замены слов длины {targetLen5} на '{substring}':\n{myText.Sentences.FirstOrDefault()}");
           
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string stopWordsFile = "stopwords_en.txt";

            if (File.Exists(stopWordsFile))
            {
                foreach (var w in File.ReadAllLines(stopWordsFile, Encoding.UTF8))
                    if (!string.IsNullOrWhiteSpace(w)) stopWords.Add(w.Trim());
                Console.WriteLine($"\n(Загружено {stopWords.Count} стоп-слов из {stopWordsFile})");
            }
            else
            {
                stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "do", "you", "a", "is", "if", "of", "the", "that"
                };
                Console.WriteLine($"\n(Файл {stopWordsFile} не найден, использован базовый список стоп-слов)");
            }
            foreach (var s in myText.Sentences)
            {
                s.Tokens.RemoveAll(t => t is Word && stopWords.Contains(t.Value));
            }
            Console.WriteLine($"6. Текст после очистки от стоп-слов:\n{myText}\n");
            XmlSerializer serializer = new XmlSerializer(typeof(Text));
            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");
            using (StreamWriter writer = new StreamWriter("output.xml", false, Encoding.UTF8))
            {
                serializer.Serialize(writer, myText, namespaces);
            }
            Console.WriteLine("7. Объект успешно сериализован в файл 'output.xml'.");
            Console.WriteLine("\nПрограмма завершена. Нажмите любую клавишу...");
            Console.ReadKey();
        }
    }
}