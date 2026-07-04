using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace EzDdd.Common;

/// <summary>
///     A thread-safe bidirectional map that maintains mappings in both directions.
///     Extends the standard dictionary functionality to support reverse lookups from value to key.
/// </summary>
/// <typeparam name="TKey">The type of keys in the map</typeparam>
/// <typeparam name="TValue">The type of values in the map</typeparam>
/// <remarks>
///     <para>
///         BiMap enforces a uniqueness constraint: each value can only be associated with one key.
///         When a value is added that already exists with a different key, the old key is automatically removed.
///         This ensures the bidirectional mapping remains consistent.
///     </para>
///     <para>
///         <b>Thread Safety:</b>
///         All operations are thread-safe through internal locking. Multiple threads can safely
///         read and write to the same BiMap instance concurrently.
///     </para>
///     <para>
///         <b>Behavior Example:</b>
///         <code>
/// var biMap = new BiMap&lt;string, int&gt;();
/// biMap.Add("key1", 100);  // key1 → 100, 100 → key1
/// biMap.Add("key2", 100);  // key2 → 100, 100 → key2, "key1" is removed
/// biMap.ContainsKey("key1"); // false
/// biMap.GetKey(100);         // "key2"
/// </code>
///     </para>
/// </remarks>
public class BiMap<TKey, TValue> : IDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> _forward;
    private readonly object _lock = new();
    private readonly Dictionary<TValue, TKey> _reverse;

    /// <summary>
    ///     Initializes a new instance of the BiMap class that is empty and uses the default equality comparers.
    /// </summary>
    public BiMap()
    {
        _forward = new Dictionary<TKey, TValue>();
        _reverse = new Dictionary<TValue, TKey>();
    }

    /// <summary>
    ///     Initializes a new instance of the BiMap class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of elements that the BiMap can contain</param>
    public BiMap(int capacity)
    {
        _forward = new Dictionary<TKey, TValue>(capacity);
        _reverse = new Dictionary<TValue, TKey>(capacity);
    }

    /// <summary>
    ///     Gets the key associated with the specified value by performing a reverse lookup.
    /// </summary>
    /// <param name="value">The value to locate in the reverse map</param>
    /// <returns>The key associated with the value, or null if the value is not found</returns>
    /// <remarks>
    ///     This is the primary additional functionality provided by BiMap over a standard dictionary.
    ///     The operation is O(1) due to the internal reverse mapping.
    /// </remarks>
    public TKey? GetKey(TValue value)
    {
        lock (_lock)
        {
            return _reverse.GetValueOrDefault(value);
        }
    }

    /// <summary>
    ///     Tries to get the key associated with the specified value.
    /// </summary>
    /// <param name="value">The value to locate</param>
    /// <param name="key">When this method returns, contains the key associated with the value, if found</param>
    /// <returns>true if the value was found; otherwise, false</returns>
    public bool TryGetKey(TValue value, [MaybeNullWhen(false)] out TKey key)
    {
        lock (_lock)
        {
            return _reverse.TryGetValue(value, out key);
        }
    }

    /// <summary>
    ///     Internal method to add or update a key-value pair with proper bidirectional mapping.
    ///     Must be called within a lock.
    /// </summary>
    /// <param name="key">The key to add or update</param>
    /// <param name="value">The value to associate with the key</param>
    /// <remarks>
    ///     <para>
    ///         This method implements a three-step algorithm to maintain bidirectional consistency:
    ///     </para>
    ///     <para>
    ///         <b>Step 1: Remove old reverse mapping if key already exists</b><br />
    ///         When updating an existing key with a new value, the old value's reverse mapping
    ///         (value → key) must be removed to prevent stale references.
    ///         Example: If "key" → 100 exists, and we're updating to "key" → 200,
    ///         remove the reverse mapping 100 → "key".
    ///     </para>
    ///     <para>
    ///         <b>Step 2: Remove old forward mapping if value already exists with different key</b><br />
    ///         BiMap enforces uniqueness constraint: each value can only map to one key.
    ///         If the new value is already associated with a different key, that old key must be removed.
    ///         Example: If "oldKey" → 100 exists, and we're adding "newKey" → 100,
    ///         remove the forward mapping "oldKey" → 100.
    ///         This ensures only "newKey" → 100 and 100 → "newKey" remain.
    ///     </para>
    ///     <para>
    ///         <b>Step 3: Create new bidirectional mapping</b><br />
    ///         Finally, establish the new bidirectional association:
    ///         forward[key] = value (key → value) and reverse[value] = key (value → key).
    ///         This ensures GetKey(value) and this[key] return consistent results.
    ///     </para>
    ///     <para>
    ///         <b>Thread Safety:</b> This method assumes the caller has acquired _lock.
    ///         It does not perform its own locking.
    ///     </para>
    /// </remarks>
    private void _AddInternal(TKey key, TValue value)
    {
        // 1. Remove old reverse mapping if key already exists
        if (_forward.TryGetValue(key, out TValue? oldValue))
        {
            _reverse.Remove(oldValue);
        }

        // 2. Remove old forward mapping if value already exists with a different key
        if (_reverse.TryGetValue(value, out TKey? oldKey) && !EqualityComparer<TKey>.Default.Equals(oldKey, key))
        {
            _forward.Remove(oldKey);
        }

        // 3. Create new bidirectional mapping
        _forward[key] = value;
        _reverse[value] = key;
    }

    #region IDictionary<TKey, TValue> Implementation

    public TValue this[TKey key]
    {
        get
        {
            lock (_lock)
            {
                return _forward[key];
            }
        }
        set
        {
            lock (_lock)
            {
                _AddInternal(key, value);
            }
        }
    }

    public ICollection<TKey> Keys
    {
        get
        {
            lock (_lock)
            {
                return _forward.Keys.ToList();
            }
        }
    }

    public ICollection<TValue> Values
    {
        get
        {
            lock (_lock)
            {
                return _forward.Values.ToList();
            }
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _forward.Count;
            }
        }
    }

    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        lock (_lock)
        {
            _AddInternal(key, value);
        }
    }

    public void Add(KeyValuePair<TKey, TValue> item)
    {
        Add(item.Key, item.Value);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _forward.Clear();
            _reverse.Clear();
        }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        lock (_lock)
        {
            return _forward.TryGetValue(item.Key, out TValue? value)
                && EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }
    }

    public bool ContainsKey(TKey key)
    {
        lock (_lock)
        {
            return _forward.ContainsKey(key);
        }
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        lock (_lock)
        {
            // Create snapshot to ensure thread-safe enumeration
            // In concurrent scenarios, the snapshot size might differ from the Count
            // that was read before CopyTo was called, so we only copy what fits
            List<KeyValuePair<TKey, TValue>> snapshot = _forward.ToList();
            int availableSpace = array.Length - arrayIndex;
            int countToCopy = Math.Min(snapshot.Count, availableSpace);

            for (int i = 0; i < countToCopy; i++)
            {
                array[arrayIndex + i] = snapshot[i];
            }
        }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        lock (_lock)
        {
            // Return a snapshot to avoid holding lock during enumeration
            return _forward.ToList().GetEnumerator();
        }
    }

    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (_forward.Remove(key, out TValue? value))
            {
                _reverse.Remove(value);
                return true;
            }

            return false;
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        lock (_lock)
        {
            if (
                _forward.TryGetValue(item.Key, out TValue? value)
                && EqualityComparer<TValue>.Default.Equals(value, item.Value)
            )
            {
                _forward.Remove(item.Key);
                _reverse.Remove(value);
                return true;
            }

            return false;
        }
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            return _forward.TryGetValue(key, out value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Additional HashMap-compatible Methods

    /// <summary>
    ///     Adds all key-value pairs from the specified dictionary to this BiMap.
    ///     For each pair, removes any existing reverse mapping to ensure bidirectional consistency.
    /// </summary>
    /// <param name="dictionary">The dictionary containing key-value pairs to add</param>
    /// <remarks>
    ///     Equivalent to Java's <c>putAll(Map&lt;? extends K, ? extends V&gt; m)</c>.
    ///     This method uses the <see cref="Add(TKey, TValue)" /> method internally,
    ///     ensuring proper bidirectional mapping for each entry.
    /// </remarks>
    public void PutAll(IDictionary<TKey, TValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        lock (_lock)
        {
            // Use Add method to ensure bidirectional mapping consistency
            foreach (KeyValuePair<TKey, TValue> entry in dictionary)
            {
                _AddInternal(entry.Key, entry.Value);
            }
        }
    }

    /// <summary>
    ///     Adds the key-value pair to the map only if the key is not already present.
    ///     If the key exists, returns the existing value without modifying the map.
    /// </summary>
    /// <param name="key">The key to add</param>
    /// <param name="value">The value to associate with the key</param>
    /// <returns>
    ///     The existing value if the key is already present, otherwise the newly added value.
    ///     Returns null if the value type is nullable and no mapping exists.
    /// </returns>
    /// <remarks>
    ///     Equivalent to Java's <c>putIfAbsent(K key, V value)</c>.
    /// </remarks>
    public TValue? PutIfAbsent(TKey key, TValue value)
    {
        lock (_lock)
        {
#pragma warning disable CS8600
            if (_forward.TryGetValue(key, out TValue existingValue))
#pragma warning restore CS8600
            {
                return existingValue;
            }

            _AddInternal(key, value);
            return value;
        }
    }

    /// <summary>
    ///     Tries to replace the value for the specified key only if the key is currently mapped.
    ///     If the key is not present, the map is not modified.
    /// </summary>
    /// <param name="key">The key whose value should be replaced</param>
    /// <param name="newValue">The new value to associate with the key</param>
    /// <param name="oldValue">
    ///     When this method returns true, contains the previous value associated with the key.
    ///     When this method returns false, contains the default value for TValue.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the key was found and the value was replaced; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method follows the .NET Try-pattern convention (similar to <c>TryGetValue</c>).
    ///         It provides safer semantics than Java's <c>replace(K key, V value)</c> which returns null,
    ///         as the Try-pattern avoids ambiguity with nullable value types.
    ///     </para>
    ///     <para>
    ///         Equivalent functionality to Java's <c>replace(K key, V value)</c>, but with .NET-idiomatic API design.
    ///     </para>
    /// </remarks>
    public bool TryReplace(TKey key, TValue newValue, out TValue oldValue)
    {
        lock (_lock)
        {
            if (_forward.TryGetValue(key, out oldValue!))
            {
                _AddInternal(key, newValue);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     Replaces the value for the specified key only if currently mapped to the specified old value.
    ///     This is an atomic compare-and-swap operation.
    /// </summary>
    /// <param name="key">The key whose value should be replaced</param>
    /// <param name="oldValue">The expected current value</param>
    /// <param name="newValue">The new value to associate with the key</param>
    /// <returns>
    ///     <c>true</c> if the value was replaced, <c>false</c> if the key was not mapped to the old value.
    /// </returns>
    /// <remarks>
    ///     Equivalent to Java's <c>replace(K key, V oldValue, V newValue)</c>.
    ///     This method is useful for implementing optimistic locking patterns.
    /// </remarks>
    public bool Replace(TKey key, TValue oldValue, TValue newValue)
    {
        lock (_lock)
        {
#pragma warning disable CS8600
            if (
                _forward.TryGetValue(key, out TValue currentValue)
#pragma warning restore CS8600
                && EqualityComparer<TValue>.Default.Equals(currentValue, oldValue)
            )
            {
                _AddInternal(key, newValue);
                return true;
            }

            return false;
        }
    }

    #endregion
}
