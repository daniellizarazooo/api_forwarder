using System.Collections.Concurrent;
using System.Security.Principal;
using Models;

namespace Store
{
    public class DictStore
    {
        private readonly ConcurrentDictionary<string, Data> _intensities = new();

        public bool KeyExists(string key)
        {
            if (_intensities.ContainsKey(key))
                return true;
            else
                return false;
        }

        public void Add(string key, string token)
        {
            _intensities.TryAdd(key, new Data { Token = token, Value = 0 });
        }

        public double? GetByKey(string key)
        {
            _intensities.TryGetValue(key, out var data);
            return data?.Value;
        }

        public void updateValue(string key, double value)
        {
            _intensities.AddOrUpdate(key,
            k => new Data { Token = string.Empty, Value = value },
            (key, existingData) =>
            {
                existingData.Value = value;
                return existingData;
            });
        }

        public ConcurrentDictionary<string, Data> GetAll()
        {
            return _intensities;
        }
    }

    public class Intensities : DictStore { }
    public class Scenes : DictStore { }
}