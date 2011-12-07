using System;
using System.Collections.Generic;
using System.Configuration;

namespace EncryptFolders.Config
{
    public class GenericConfigurationElementCollection<T> : ConfigurationElementCollection, IEnumerable<T>
        where T : ConfigurationElement, new()
    {
        private readonly List<T> _elements = new List<T>();

        public string[] ToStringArray()
        {
            var array = new string[_elements.Count];

            int count = 0;

            foreach (var element in _elements)
            {
                array[count] = Environment.ExpandEnvironmentVariables(element.ToString());
                
                count++;
            }

            return array;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            var newElement = new T();

            _elements.Add(newElement);
            
            return newElement;
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return _elements.Find(e => e.Equals(element));
        }

        public new IEnumerator<T> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }
    }
}