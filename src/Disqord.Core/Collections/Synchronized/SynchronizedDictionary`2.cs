﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Disqord.Collections.Synchronized
{
    public class SynchronizedDictionary<TKey, TValue> : ISynchronizedDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        public ICollection<TKey> Keys
        {
            get
            {
                lock (this)
                {
                    var array = new TKey[_dictionary.Count];
                    _dictionary.Keys.CopyTo(array, 0);
                    return array;
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                lock (this)
                {
                    var array = new TValue[_dictionary.Count];
                    _dictionary.Values.CopyTo(array, 0);
                    return array;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (this)
                {
                    return _dictionary.Count;
                }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (this)
                {
                    return _dictionary[key];
                }
            }
            set
            {
                lock (this)
                {
                    _dictionary[key] = value;
                }
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        private readonly Dictionary<TKey, TValue> _dictionary;

        public SynchronizedDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public SynchronizedDictionary(int capacity)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public SynchronizedDictionary(Dictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            lock (this)
            {
                if (_dictionary.TryGetValue(key, out value))
                    return value;

                _dictionary.Add(key, value);
                return value;
            }
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            lock (this)
            {
                if (_dictionary.TryGetValue(key, out var value))
                    return value;

                value = factory(key);
                _dictionary.Add(key, value);
                return value;
            }
        }

        public TValue GetOrAdd<TState>(TKey key, Func<TKey, TState, TValue> factory, TState state)
        {
            lock (this)
            {
                if (_dictionary.TryGetValue(key, out var value))
                    return value;

                value = factory(key, state);
                _dictionary.Add(key, value);
                return value;
            }
        }

        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateFactory)
        {
            lock (this)
            {
                if (_dictionary.TryGetValue(key, out var value))
                {
                    value = updateFactory(key, value);
                    _dictionary[key] = value;
                    return value;
                }
                else
                {
                    _dictionary.Add(key, addValue);
                    return addValue;
                }
            }
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory)
        {
            lock (this)
            {
                if (_dictionary.TryGetValue(key, out var value))
                {
                    value = updateFactory(key, value);
                    _dictionary[key] = value;
                    return value;
                }
                else
                {
                    value = addFactory(key);
                    _dictionary.Add(key, value);
                    return value;
                }
            }
        }

        public TValue AddOrUpdate<TState>(TKey key, Func<TKey, TState, TValue> addFactory, Func<TKey, TState, TValue, TValue> updateFactory, TState state)
        {
            lock (this)
            {
                if (_dictionary.TryGetValue(key, out var value))
                {
                    value = updateFactory(key, state, value);
                    _dictionary[key] = value;
                    return value;
                }
                else
                {
                    value = addFactory(key, state);
                    _dictionary.Add(key, value);
                    return value;
                }
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            lock (this)
            {
                return _dictionary.TryAdd(key, value);
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            lock (this)
            {
                return _dictionary.Remove(key, out value);
            }
        }

        public void EnsureCapacity(int capacity)
        {
            lock (this)
            {
                _dictionary.EnsureCapacity(capacity);
            }
        }

        public void TrimExcess()
        {
            lock (this)
            {
                _dictionary.TrimExcess();
            }
        }

        public void TrimExcess(int capacity)
        {
            lock (this)
            {
                _dictionary.TrimExcess(capacity);
            }
        }

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            lock (this)
            {
                var array = new KeyValuePair<TKey, TValue>[_dictionary.Count];
                (_dictionary as IDictionary<TKey, TValue>).CopyTo(array, 0);
                return array;
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (this)
            {
                _dictionary.Add(key, value);
            }
        }

        public void Clear()
        {
            lock (this)
            {
                _dictionary.Clear();
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (this)
            {
                return _dictionary.ContainsKey(key);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            lock (this)
            {
                (_dictionary as IDictionary<TKey, TValue>).CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(TKey key)
        {
            lock (this)
            {
                return _dictionary.Remove(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (this)
            {
                return _dictionary.TryGetValue(key, out value);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            var list = (IList<KeyValuePair<TKey, TValue>>) ToArray();
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => ContainsKey(item.Key);

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
            => Remove(item.Key);
    }
}