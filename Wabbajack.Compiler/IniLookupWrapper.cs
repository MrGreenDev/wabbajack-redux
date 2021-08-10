using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IniParser.Model;

namespace Wabbajack.Compiler
{
    public class IniLookupWrapper : IReadOnlyDictionary<string, string>
    {
        private readonly KeyDataCollection _collection;

        public IniLookupWrapper(KeyDataCollection collection)
        {
            _collection = collection;
        }


        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count { get; }
        public bool ContainsKey(string key)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetValue(string key, out string value)
        {
            throw new System.NotImplementedException();
        }

        public string this[string key] => throw new System.NotImplementedException();

        public IEnumerable<string> Keys { get; }
        public IEnumerable<string> Values { get; }
    }
}