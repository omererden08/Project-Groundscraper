using System;
using System.Collections.Generic;

public class MinHeap<T> where T : IHeapItem<T>
{
    private T[] items;
    public int Count { get; private set; }

    public MinHeap(int maxSize)
    {
        items = new T[maxSize];
    }

    public void Add(T item)
    {
        item.HeapIndex = Count;
        items[Count] = item;
        SortUp(item);
        Count++;
    }

    public T RemoveFirst()
    {
        T first = items[0];
        Count--;
        items[0] = items[Count];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return first;
    }

    public void UpdateItem(T item) => SortUp(item);
    public bool Contains(T item) => Equals(items[item.HeapIndex], item);

    void SortDown(T item)
    {
        while (true)
        {
            int left = item.HeapIndex * 2 + 1;
            int right = item.HeapIndex * 2 + 2;
            int swapIndex = 0;

            if (left < Count)
            {
                swapIndex = left;

                if (right < Count && items[right].CompareTo(items[left]) < 0)
                    swapIndex = right;

                if (items[swapIndex].CompareTo(item) < 0)
                    Swap(item, items[swapIndex]);
                else return;
            }
            else return;
        }
    }

    void SortUp(T item)
    {
        int parent = (item.HeapIndex - 1) / 2;

        while (item.HeapIndex > 0)
        {
            if (items[parent].CompareTo(item) > 0)
            {
                Swap(item, items[parent]);
                parent = (item.HeapIndex - 1) / 2;
            }
            else break;
        }
    }

    void Swap(T a, T b)
    {
        items[a.HeapIndex] = b;
        items[b.HeapIndex] = a;

        int temp = a.HeapIndex;
        a.HeapIndex = b.HeapIndex;
        b.HeapIndex = temp;
    }
}
