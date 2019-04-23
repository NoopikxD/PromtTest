using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Runtime.Serialization;
using CSharpTest.Net.Collections;
using CSharpTest.Net.IO;
using System.Text.RegularExpressions;
using CSharpTest.Net.Serialization;
using System.IO;
namespace CorpusSearch
{
    [Serializable]
    public class SampleKey: ISerializable, IComparable<SampleKey>
    {
        
        public int vocabID { get; set; }
        public int segID1 { get; set; }
        public int segID2 { get; set; }
        public SampleKey(int _vocabID,int _segID1, int _segID2)
        {
            vocabID = _vocabID;
            segID1 = _segID1;
            segID2 = _segID2;
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Use the AddValue method to specify serialized values.
            info.AddValue("vocabID", vocabID, typeof(int));
            info.AddValue("segID1", segID1, typeof(int));
            info.AddValue("segID2", segID2, typeof(int));
        }
        public SampleKey(SerializationInfo info, StreamingContext context)
        {
            this.vocabID = (int)info.GetValue("vocabID", typeof(int));
            this.segID1 = (int)info.GetValue("segID1", typeof(int));
            this.segID2 = (int)info.GetValue("segID2", typeof(int));
        }
        public int CompareTo(SampleKey obj)
        {

            if (this.vocabID == obj.vocabID && this.segID1 == obj.segID1 && this.segID2 == obj.segID2)
                return 0;
            else
            {
                if(this.vocabID==obj.vocabID)
                {
                    return this.segID1 > obj.segID1 ? 1 : -1;
                }
                else
                {
                    return this.vocabID > obj.vocabID ? 1 : -1;
                }
            }
                
        }
        public string ToString()
        {
            return this.vocabID.ToString() + " " + this.segID1 + " " + this.segID2;
        }
    }
    public class SampleKeySerializer : ISerializer<SampleKey>
    {
        SampleKey ISerializer<SampleKey>.ReadFrom(System.IO.Stream stream)
        {
            var ret = new SampleKey(PrimitiveSerializer.Int32.ReadFrom(stream), PrimitiveSerializer.Int32.ReadFrom(stream), PrimitiveSerializer.Int32.ReadFrom(stream));
            return ret;
        }
        void ISerializer<SampleKey>.WriteTo(SampleKey value, System.IO.Stream stream)
        {
            PrimitiveSerializer.Int32.WriteTo(value.vocabID,stream);
            PrimitiveSerializer.Int32.WriteTo(value.segID1, stream);
            PrimitiveSerializer.Int32.WriteTo(value.segID2, stream);

        }
    }
    class Program
    {
        static public void Add(string pathToBD, string pathToTMX)
        {
            List<string> eng = new List<string>();
            List<string> ru = new List<string>();   
            XmlTextReader reader = new XmlTextReader(pathToTMX);
            while (reader.Read())
            {

                if (reader.XmlLang == "en" && reader.NodeType == XmlNodeType.Text)
                {
                    eng.Add(reader.Value);
                }
                if (reader.XmlLang == "ru" && reader.NodeType == XmlNodeType.Text)
                    ru.Add(reader.Value);


            }
            BPlusTree<int, string>.OptionsV2 optionsTexts = new BPlusTree<int, string>.OptionsV2(

                PrimitiveSerializer.Int32, PrimitiveSerializer.String);
            optionsTexts.CreateFile = CreatePolicy.IfNeeded;
            optionsTexts.FileName = (pathToBD + "\\texts.dat");
           

            BPlusTree<string, int>.OptionsV2 optionsVocab = new BPlusTree<string, int>.OptionsV2(

                PrimitiveSerializer.String, PrimitiveSerializer.Int32);
            optionsVocab.CreateFile = CreatePolicy.IfNeeded;
            optionsVocab.FileName = (pathToBD + "\\vocab.dat");

            BPlusTree<SampleKey, int>.OptionsV2 optionsSample = new BPlusTree<SampleKey, int>.OptionsV2(

              new SampleKeySerializer(), PrimitiveSerializer.Int32);
            optionsSample.CreateFile = CreatePolicy.IfNeeded;
            optionsSample.FileName = (pathToBD + "\\samples.dat");

            BPlusTree<int, string> texts = new BPlusTree<int, string>(optionsTexts);
            BPlusTree<string, int> vocab = new BPlusTree<string, int>(optionsVocab);
            BPlusTree<SampleKey, int> samples = new BPlusTree<SampleKey, int>(optionsSample);
            texts.EnableCount();
            vocab.EnableCount();
            samples.EnableCount();
            for (int i = 0; i < eng.Count; i++)
            {
                var clearStringEng = Regex.Replace(eng[i], "[^A-Za-z0-9 ]", "");
                int textPosition = texts.Keys.Count + i;
                texts.TryAdd(textPosition, eng[i]);

                var clearStringRu = Regex.Replace(ru[i], "[^А-Яа-я0-9 ]", "");
                texts.TryAdd(textPosition + eng.Count, ru[i]);
                var temp1 = clearStringEng.Split(' ');
                var temp2 = clearStringRu.Split(' ');
                for (int j = 0; j < temp1.Length; j++)
                {
                    vocab.TryAdd(temp1[j].ToLower(), vocab.Keys.Count + 1);
                    samples.TryAdd(new SampleKey(vocab[temp1[j].ToLower()], textPosition, textPosition + eng.Count), 0);
                    
                }
                for (int j = 0; j < temp2.Length; j++)
                {
                    vocab.TryAdd(temp2[j].ToLower(), vocab.Keys.Count + 1);
                    samples.TryAdd(new SampleKey(vocab[temp2[j].ToLower()], textPosition, textPosition + eng.Count), 0);


                }


            }

        }
        static public void Find(string pathToBD, string pathToHTML, string word)
        {
            BPlusTree<int, string>.OptionsV2 optionsTexts = new BPlusTree<int, string>.OptionsV2(

                PrimitiveSerializer.Int32, PrimitiveSerializer.String);
            optionsTexts.CreateFile = CreatePolicy.IfNeeded;
            optionsTexts.FileName = (pathToBD+"\\texts.dat");

            BPlusTree<string, int>.OptionsV2 optionsVocab = new BPlusTree<string, int>.OptionsV2(

                PrimitiveSerializer.String, PrimitiveSerializer.Int32);
            optionsVocab.CreateFile = CreatePolicy.IfNeeded;
            optionsVocab.FileName = (pathToBD + "\\vocab.dat");

            BPlusTree<SampleKey, int>.OptionsV2 optionsSample = new BPlusTree<SampleKey, int>.OptionsV2(

              new SampleKeySerializer(), PrimitiveSerializer.Int32);
            optionsSample.CreateFile = CreatePolicy.IfNeeded;
            optionsSample.FileName = (pathToBD + "\\samples.dat");

            BPlusTree<int, string> texts = new BPlusTree<int, string>(optionsTexts);
            BPlusTree<string, int> vocab = new BPlusTree<string, int>(optionsVocab);
            BPlusTree<SampleKey, int> samples = new BPlusTree<SampleKey, int>(optionsSample);
            texts.EnableCount();
            vocab.EnableCount();
            samples.EnableCount();

            
            string result = "<html><body><table border= \"1px solid black\">";

            foreach (var kv in samples.EnumerateRange(new SampleKey(vocab[word], 0, texts.Keys.Count), new SampleKey(vocab[word], texts.Keys.Count, texts.Keys.Count * 2)))
            {
                result += "<tr><td>" + texts[kv.Key.segID1]+"</td>";
                result += "<td>" + texts[kv.Key.segID2] + "</td></tr>";
                //Console.WriteLine(texts[kv.Key.segID1]);
                //Console.WriteLine(kv.Key.ToString());
            }
            result += "</table></body></html>";
            File.WriteAllText(pathToHTML, result);
        }
        
        static void Main(string[] args)
        {
            var arg = args[1].Split(' ');

            if(args.Length==3)
            {
                Console.WriteLine("Adding");
                Add(args[1], args[2]);
                
            }
            else if(args.Length==4)
            {
                Console.WriteLine("Finding");
                Find(args[1], args[2], args[3]);
            }
            /*using(TempFile tmp = TempFile.Attach("C:\\test\\texts.dat"))
            {
                var options = new TransactionLogOptions<int, string>(tmp.TempPath,PrimitiveSerializer.Int32,PrimitiveSerializer.String);
                using (var trans = new TransactionLog<int, string>(options))
                {
                    var token = trans.BeginTransaction();
                    foreach(var k in texts)
                        trans.AddValue(ref token, k.Key, k.Value);
                    trans.CommitTransaction(ref token);
                }
                
            }*/
            Console.WriteLine("Jobs done. Press any key.");
                Console.ReadLine();
        }
    }
}
