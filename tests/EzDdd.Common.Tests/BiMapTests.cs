namespace EzDdd.Common.Tests;

public class BiMapTests
{
    #region Add and Construction Tests

    [Fact]
    public void Add_AndGet_WorksBidirectionally()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        BiMap<string, int> biMap = new();

        biMap.Add("one", 1);
        biMap.Add("two", 2);
        biMap.Add("three", 3);

        Assert.Equal(1, biMap["one"]);
        Assert.Equal(2, biMap["two"]);
        Assert.Equal(3, biMap["three"]);
        Assert.Equal("one", biMap.GetKey(1));
        Assert.Equal("two", biMap.GetKey(2));
        Assert.Equal("three", biMap.GetKey(3));
    }

    [Fact]
    public void Add_WhenValueChanges_UpdatesReverseMappingCorrectly()
    {
        BiMap<string, int> biMap = new() { { "key", 1 } };

        biMap["key"] = 2;

        Assert.Null(biMap.GetKey(1));
        Assert.Equal("key", biMap.GetKey(2));
    }

    [Fact]
    public void Add_WhenSameValueHasDifferentKey_RemovesOldKey()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        BiMap<string, int> biMap = new() { { "key1", 100 } };

        biMap.Add("key2", 100);

        Assert.Equal("key2", biMap.GetKey(100));
        Assert.False(biMap.ContainsKey("key1"));
        Assert.Equal(100, biMap["key2"]);
        Assert.Single(biMap);
    }

    [Fact]
    public void Constructor_WhenProvidingCapacity_CreatesEmptyBiMap()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        BiMap<string, int> biMap = new(100);

        Assert.Empty(biMap);
    }

    #endregion

    #region TryGetValue and GetKey Tests

    [Fact]
    public void TryGetValue_WhenKeyExists_ReturnsTrue()
    {
        BiMap<string, int> biMap = new() { { "key", 42 } };

        bool found = biMap.TryGetValue("key", out int value);

        Assert.True(found);
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_WhenKeyNotExists_ReturnsFalse()
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        BiMap<string, int> biMap = new();

        bool found = biMap.TryGetValue("missing", out int value);

        Assert.False(found);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGetKey_WhenValueExists_ReturnsTrue()
    {
        BiMap<string, int> biMap = new() { { "key", 42 } };

        bool found = biMap.TryGetKey(42, out string? key);

        Assert.True(found);
        Assert.Equal("key", key);
    }

    [Fact]
    public void TryGetKey_WhenValueNotExists_ReturnsFalse()
    {
        BiMap<string, int> biMap = new();

        bool found = biMap.TryGetKey(999, out string? key);

        Assert.False(found);
        Assert.Null(key);
    }

    [Fact]
    public void GetKey_WhenValueNotExists_ReturnsNull()
    {
        BiMap<string, int> biMap = new() { { "key", 42 } };

        string? key = biMap.GetKey(999);

        Assert.Null(key);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_RemovesBidirectionally()
    {
        BiMap<string, int> biMap = new() { { "one", 1 }, { "two", 2 } };

        bool removed = biMap.Remove("one");

        Assert.True(removed);
        Assert.False(biMap.ContainsKey("one"));
        Assert.Null(biMap.GetKey(1));
        Assert.Equal(2, biMap["two"]);
        Assert.Equal("two", biMap.GetKey(2));
    }

    [Fact]
    public void Remove_WhenUsingKeyValuePair_RemovesOnlyIfMatches()
    {
        BiMap<string, int> biMap = new() { { "key", 42 } };

        bool removed1 = biMap.Remove(new KeyValuePair<string, int>("key", 99));
        Assert.False(removed1);
        Assert.Single(biMap);

        bool removed2 = biMap.Remove(new KeyValuePair<string, int>("key", 42));
        Assert.True(removed2);
        Assert.Empty(biMap);
    }

    #endregion

    #region Contains Tests

    [Fact]
    public void Contains_WhenUsingKeyValuePair_ReturnsCorrectResult()
    {
        BiMap<string, int> biMap = new() { { "key", 42 } };

        Assert.Contains(new KeyValuePair<string, int>("key", 42), biMap);
        Assert.DoesNotContain(new KeyValuePair<string, int>("key", 99), biMap);
        Assert.DoesNotContain(new KeyValuePair<string, int>("other", 42), biMap);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllMappings()
    {
        BiMap<string, int> biMap = new()
        {
            { "one", 1 },
            { "two", 2 },
            { "three", 3 },
        };

        biMap.Clear();

        Assert.Empty(biMap);
        Assert.False(biMap.ContainsKey("one"));
        Assert.Null(biMap.GetKey(1));
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void GetEnumerator_ReturnsAllEntries()
    {
        BiMap<string, int> biMap = new()
        {
            { "one", 1 },
            { "two", 2 },
            { "three", 3 },
        };

        List<KeyValuePair<string, int>> entries = biMap.ToList();

        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, e => e.Key == "one" && e.Value == 1);
        Assert.Contains(entries, e => e.Key == "two" && e.Value == 2);
        Assert.Contains(entries, e => e.Key == "three" && e.Value == 3);
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        BiMap<string, int> biMap = new() { { "one", 1 }, { "two", 2 } };

        ICollection<string> keys = biMap.Keys;

        Assert.Equal(2, keys.Count);
        Assert.Contains("one", keys);
        Assert.Contains("two", keys);
    }

    [Fact]
    public void Values_ReturnsAllValues()
    {
        BiMap<string, int> biMap = new() { { "one", 1 }, { "two", 2 } };

        ICollection<int> values = biMap.Values;

        Assert.Equal(2, values.Count);
        Assert.Contains(1, values);
        Assert.Contains(2, values);
    }

    #endregion

    #region CopyTo Tests

    [Fact]
    public void CopyTo_CopiesAllEntriesToArray()
    {
        BiMap<string, int> biMap = new() { { "one", 1 }, { "two", 2 } };
        KeyValuePair<string, int>[] array = new KeyValuePair<string, int>[2];

        biMap.CopyTo(array, 0);

        Assert.Equal(2, array.Length);
        Assert.Contains(new KeyValuePair<string, int>("one", 1), array);
        Assert.Contains(new KeyValuePair<string, int>("two", 2), array);
    }

    #endregion

    #region PutAll Tests

    [Fact]
    public void PutAll_WhenAddingMultipleEntries_AddsAllBidirectionally()
    {
        BiMap<string, int> biMap = new() { { "existing", 0 } };
        Dictionary<string, int> toAdd = new()
        {
            { "one", 1 },
            { "two", 2 },
            { "three", 3 },
        };

        biMap.PutAll(toAdd);

        Assert.Equal(4, biMap.Count);
        Assert.Equal(1, biMap["one"]);
        Assert.Equal(2, biMap["two"]);
        Assert.Equal(3, biMap["three"]);
        Assert.Equal("one", biMap.GetKey(1));
        Assert.Equal("two", biMap.GetKey(2));
        Assert.Equal("three", biMap.GetKey(3));
    }

    [Fact]
    public void PutAll_WhenAddingEmptyDictionary_DoesNotModifyBiMap()
    {
        BiMap<string, int> biMap = new() { { "key", 42 } };
        Dictionary<string, int> empty = new();

        biMap.PutAll(empty);

        Assert.Single(biMap);
        Assert.Equal(42, biMap["key"]);
    }

    [Fact]
    public void PutAll_WhenValueConflicts_RemovesOldKey()
    {
        BiMap<string, int> biMap = new() { { "oldKey", 100 } };
        Dictionary<string, int> toAdd = new() { { "newKey", 100 } };

        biMap.PutAll(toAdd);

        Assert.Single(biMap);
        Assert.False(biMap.ContainsKey("oldKey"));
        Assert.Equal(100, biMap["newKey"]);
        Assert.Equal("newKey", biMap.GetKey(100));
    }

    #endregion

    #region PutIfAbsent Tests

    [Fact]
    public void PutIfAbsent_WhenKeyDoesNotExist_AddsAndReturnsValue()
    {
        BiMap<string, int> biMap = new();

        int? result = biMap.PutIfAbsent("key", 42);

        Assert.Equal(42, result);
        Assert.Equal(42, biMap["key"]);
        Assert.Equal("key", biMap.GetKey(42));
    }

    [Fact]
    public void PutIfAbsent_WhenKeyExists_ReturnsExistingValueWithoutModifying()
    {
        BiMap<string, int> biMap = new() { { "key", 100 } };

        int? result = biMap.PutIfAbsent("key", 200);

        Assert.Equal(100, result);
        Assert.Equal(100, biMap["key"]);
        Assert.Equal("key", biMap.GetKey(100));
        Assert.Null(biMap.GetKey(200));
    }

    #endregion

    #region TryReplace Tests

    [Fact]
    public void TryReplace_WhenKeyExists_ReplacesAndReturnsTrueWithOldValue()
    {
        BiMap<string, int> biMap = new() { { "key", 100 } };

        bool replaced = biMap.TryReplace("key", 200, out int oldValue);

        Assert.True(replaced);
        Assert.Equal(100, oldValue);
        Assert.Equal(200, biMap["key"]);
        Assert.Equal("key", biMap.GetKey(200));
        Assert.Null(biMap.GetKey(100));
    }

    [Fact]
    public void TryReplace_WhenKeyDoesNotExist_ReturnsFalseWithoutModifying()
    {
        BiMap<string, int> biMap = new() { { "existing", 100 } };

        bool replaced = biMap.TryReplace("missing", 200, out int oldValue);

        Assert.False(replaced);
        Assert.Equal(0, oldValue); // default(int)
        Assert.Single(biMap);
        Assert.False(biMap.ContainsKey("missing"));
        Assert.Null(biMap.GetKey(200));
    }

    [Fact]
    public void TryReplace_WhenReplacingValue_UpdatesReverseMappingCorrectly()
    {
        BiMap<string, int> biMap = new() { { "key1", 100 }, { "key2", 200 } };

        bool replaced = biMap.TryReplace("key1", 300, out int oldValue);

        Assert.True(replaced);
        Assert.Equal(100, oldValue);
        Assert.Equal(300, biMap["key1"]);
        Assert.Equal("key1", biMap.GetKey(300));
        Assert.Null(biMap.GetKey(100));
        Assert.Equal(200, biMap["key2"]);
    }

    #endregion

    #region Replace Tests

    [Fact]
    public void ReplaceConditional_WhenOldValueMatches_ReplacesAndReturnsTrue()
    {
        BiMap<string, int> biMap = new() { { "key", 100 } };

        bool replaced = biMap.Replace("key", 100, 200);

        Assert.True(replaced);
        Assert.Equal(200, biMap["key"]);
        Assert.Equal("key", biMap.GetKey(200));
        Assert.Null(biMap.GetKey(100));
    }

    [Fact]
    public void ReplaceConditional_WhenOldValueDoesNotMatch_ReturnsFalseWithoutModifying()
    {
        BiMap<string, int> biMap = new() { { "key", 100 } };

        bool replaced = biMap.Replace("key", 999, 200);

        Assert.False(replaced);
        Assert.Equal(100, biMap["key"]);
        Assert.Equal("key", biMap.GetKey(100));
        Assert.Null(biMap.GetKey(200));
    }

    [Fact]
    public void ReplaceConditional_WhenKeyDoesNotExist_ReturnsFalseWithoutModifying()
    {
        BiMap<string, int> biMap = new() { { "existing", 100 } };

        bool replaced = biMap.Replace("missing", 100, 200);

        Assert.False(replaced);
        Assert.Single(biMap);
        Assert.False(biMap.ContainsKey("missing"));
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ConcurrentAdds_MaintainConsistency()
    {
        BiMap<string, int> biMap = new();
        const int threadCount = 100;
        const int operationsPerThread = 100;

        IEnumerable<Task> tasks = Enumerable
            .Range(0, threadCount)
            .Select(threadId =>
                Task.Run(() =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        string key = $"thread{threadId}_key{j}";
                        int value = (threadId * 1000) + j;
                        biMap.Add(key, value);
                    }
                })
            );

        await Task.WhenAll(tasks);

        Assert.Equal(threadCount * operationsPerThread, biMap.Count);

        // Verify bidirectional consistency
        for (int i = 0; i < threadCount; i++)
        {
            for (int j = 0; j < operationsPerThread; j++)
            {
                string key = $"thread{i}_key{j}";
                int value = (i * 1000) + j;
                Assert.Equal(value, biMap[key]);
                Assert.Equal(key, biMap.GetKey(value));
            }
        }
    }

    [Fact]
    public async Task ConcurrentRemoves_MaintainConsistency()
    {
        BiMap<string, int> biMap = new();
        const int entryCount = 1000;

        // Add initial entries
        for (int i = 0; i < entryCount; i++)
        {
            biMap.Add($"key{i}", i);
        }

        const int threadCount = 10;
        int removeCount = 0;

        IEnumerable<Task> tasks = Enumerable
            .Range(0, threadCount)
            .Select(threadId =>
                Task.Run(() =>
                {
                    for (int j = threadId; j < entryCount; j += threadCount)
                    {
                        if (biMap.Remove($"key{j}"))
                        {
                            Interlocked.Increment(ref removeCount);
                        }
                    }
                })
            );

        await Task.WhenAll(tasks);

        Assert.Empty(biMap);
        Assert.Equal(entryCount, removeCount);

        // Verify all reverse mappings are removed
        for (int i = 0; i < entryCount; i++)
        {
            Assert.Null(biMap.GetKey(i));
        }
    }

    [Fact]
    public async Task ConcurrentPutAndRemove_MaintainConsistency()
    {
        BiMap<string, int> biMap = new();
        const int operationCount = 10000;

        IEnumerable<Task> addTasks = Enumerable
            .Range(0, operationCount)
            .Select(i => Task.Run(() => biMap.Add($"key{i}", i)));

        IEnumerable<Task<bool>> removeTasks = Enumerable
            .Range(0, operationCount)
            .Select(i => Task.Run(() => biMap.Remove($"key{i}")));

        await Task.WhenAll(addTasks.Concat(removeTasks));

        // Verify consistency - for each remaining entry, reverse mapping should exist
        foreach (KeyValuePair<string, int> entry in biMap)
        {
            Assert.Equal(entry.Key, biMap.GetKey(entry.Value));
        }
    }

    [Fact]
    public async Task ConcurrentClearOperations_Complete()
    {
        const int threadCount = 10;

        IEnumerable<Task> tasks = Enumerable
            .Range(0, threadCount)
            .Select(_ =>
                Task.Run(() =>
                {
                    BiMap<string, int> biMap = new();

                    // Add initial data
                    for (int i = 0; i < 100; i++)
                    {
                        biMap.Add($"key{i}", i);
                    }

                    biMap.Clear();

                    Assert.Empty(biMap);

                    // Verify no stale reverse mappings
                    for (int i = 0; i < 100; i++)
                    {
                        Assert.Null(biMap.GetKey(i));
                    }
                })
            );

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ConcurrentReadsAndWrites_MaintainConsistency()
    {
        BiMap<string, int> biMap = new();
        const int writeCount = 1000;
        const int readCount = 1000;
        int successfulReads = 0;

        IEnumerable<Task> writeTasks = Enumerable
            .Range(0, writeCount)
            .Select(i => Task.Run(() => biMap.Add($"key{i}", i)));

        IEnumerable<Task> readTasks = Enumerable
            .Range(0, readCount)
            .Select(i =>
                Task.Run(() =>
                {
                    if (biMap.TryGetValue($"key{i}", out int value) && value == i)
                    {
                        if (biMap.TryGetKey(value, out string? key) && key == $"key{i}")
                        {
                            Interlocked.Increment(ref successfulReads);
                        }
                    }
                })
            );

        await Task.WhenAll(writeTasks.Concat(readTasks));

        // Some reads should have succeeded
        Assert.True(successfulReads > 0);

        // Final consistency check
        foreach (KeyValuePair<string, int> entry in biMap)
        {
            Assert.Equal(entry.Key, biMap.GetKey(entry.Value));
        }
    }

    [Fact]
    public async Task ConcurrentValueOverwrites_MaintainConsistency()
    {
        BiMap<string, int> biMap = new();
        const int threadCount = 100;

        // Multiple threads try to update the same keys with different values
        IEnumerable<Task> tasks = Enumerable
            .Range(0, threadCount)
            .Select(value =>
                Task.Run(() =>
                {
                    for (int key = 0; key < 10; key++)
                    {
                        biMap[$"key{key}"] = value;
                    }
                })
            );

        await Task.WhenAll(tasks);

        // Due to BiMap's behavior, when multiple keys have same value,
        // only the last key remains
        Assert.True(biMap.Count >= 1 && biMap.Count <= 10);

        // Verify consistency - for remaining entries
        foreach (KeyValuePair<string, int> entry in biMap)
        {
            string? reverseKey = biMap.GetKey(entry.Value);
            Assert.Equal(entry.Key, reverseKey);
            Assert.Equal(entry.Value, biMap[reverseKey!]);
        }
    }

    [Fact]
    public async Task ConcurrentEnumeration_DoesNotThrow()
    {
        BiMap<string, int> biMap = new();

        // Add initial data
        for (int i = 0; i < 100; i++)
        {
            biMap.Add($"key{i}", i);
        }

        Task enumerationTask = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                List<KeyValuePair<string, int>> snapshot = biMap.ToList();
                Assert.NotEmpty(snapshot);
            }
        });

        Task modificationTask = Task.Run(() =>
        {
            for (int i = 100; i < 200; i++)
            {
                biMap.Add($"key{i}", i);
            }
        });

        await Task.WhenAll(enumerationTask, modificationTask);
    }

    #endregion
}
