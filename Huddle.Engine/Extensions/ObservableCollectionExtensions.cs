using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Huddle.Engine.Extensions
{
    public static class ObservableCollectionExtensions
    {
        public static void AddAll<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Add(item);
        }

        public static int RemoveAll<T>(this ObservableCollection<T> collection, Func<T, bool> condition)
        {
            var itemsToRemove = collection.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
                collection.Remove(itemToRemove);

            return itemsToRemove.Count;
        }

        public static void RemoveAll<T>(this ObservableCollection<T> collection, IEnumerable<T> itemsToRemove)
        {
            foreach (var itemToRemove in itemsToRemove)
                collection.Remove(itemToRemove);
        }
    }
}
