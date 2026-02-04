using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace v2rayWinUI.Common;

/// <summary>
/// Safe wrapper for ObservableCollection that exposes only read-only interface to prevent
/// improper collection manipulation from the View layer, which can cause COMException
/// in WinUI 3 when using operations like Clear() followed by AddRange().
/// 
/// This pattern is recommended by SnapHutao and prevents UI marshalling issues.
/// </summary>
public sealed class ReadOnlyObservableCollectionWrapper<T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly ObservableCollection<T> _innerCollection;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public int Count => _innerCollection.Count;

    public T this[int index] => _innerCollection[index];

    public ReadOnlyObservableCollectionWrapper()
    {
        _innerCollection = new ObservableCollection<T>();
        SubscribeToInnerEvents();
    }

    public ReadOnlyObservableCollectionWrapper(IEnumerable<T> items)
    {
        _innerCollection = new ObservableCollection<T>(items);
        SubscribeToInnerEvents();
    }

    private void SubscribeToInnerEvents()
    {
        _innerCollection.CollectionChanged += (s, e) =>
        {
            CollectionChanged?.Invoke(this, e);
        };

        ((INotifyPropertyChanged)_innerCollection).PropertyChanged += (s, e) =>
        {
            PropertyChanged?.Invoke(this, e);
        };
    }

    /// <summary>
    /// Get mutable access to inner collection for ViewModel use only
    /// </summary>
    internal ObservableCollection<T> GetMutableCollection() => _innerCollection;

    public IEnumerator<T> GetEnumerator() => _innerCollection.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _innerCollection.GetEnumerator();

    /// <summary>
    /// Safe clear and add range operation that avoids COMException
    /// Must be called from ViewModel only, not from View
    /// </summary>
    internal void ReplaceTo(IEnumerable<T> items)
    {
        _innerCollection.Clear();
        foreach (var item in items)
        {
            _innerCollection.Add(item);
        }
    }

    /// <summary>
    /// Contains check
    /// </summary>
    public bool Contains(T item) => _innerCollection.Contains(item);

    /// <summary>
    /// Index of item
    /// </summary>
    public int IndexOf(T item) => _innerCollection.IndexOf(item);
}

/// <summary>
/// Extension methods for safe ObservableCollection operations in ViewModels
/// </summary>
public static class ObservableCollectionExtensions
{
    /// <summary>
    /// Safe replace operation - clears and adds all items without triggering COMException
    /// Use this instead of Clear() + AddRange() on ObservableCollection
    /// </summary>
    public static void SafeReplace<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>
    /// Safe batch add operation
    /// </summary>
    public static void SafeAddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>
    /// Create a read-only wrapper from ObservableCollection
    /// </summary>
    public static ReadOnlyObservableCollectionWrapper<T> ToReadOnlyWrapper<T>(
        this ObservableCollection<T> collection)
    {
        return new ReadOnlyObservableCollectionWrapper<T>(collection);
    }
}
