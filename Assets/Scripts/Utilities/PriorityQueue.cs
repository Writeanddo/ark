using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Creates a queue where by using IComparable.CompareTo it determines 
/// where on the queue an item is to be placed thus keeping the queue sorted by priority
/// </summary>
/// <typeparam name="T"></typeparam>
public class PriorityQueue<T> where T : IComparable<T>
{
    List<T> data;

    /// <summary>
    /// Returns the total amount of items in the list
    /// </summary>
    public int Count { get { return data.Count; } }

    public PriorityQueue()
    {
        this.data = new List<T>();
    }

    /// <summary>
    /// Adds the given item to the list
    /// </summary>
    /// <param name="item"></param>
    public void Enqueue(T item)
    {
        data.Add(item);

        int childIndex = data.Count - 1;

        while (childIndex > 0)
        {
            int parentIndex = (childIndex - 1) / 2;

            // 0 = same priority
            // 1 = parent's priority is greater
            // both cases, end the loop
            if (data[childIndex].CompareTo(data[parentIndex]) >= 0)
            {
                break;
            }

            T tmp = data[childIndex];
            data[childIndex] = data[parentIndex];
            data[parentIndex] = tmp;

            childIndex = parentIndex;
        }
    }

    /// <summary>
    /// Returns the first item on the list
    /// Partially restores remaining items to ensure the next item is still 
    /// the one with the highest priority
    /// </summary>
    /// <returns></returns>
    public T Dequeue()
    {
        int lastIndex = data.Count - 1;
        int parentIndex = 0;
        T frontItem = data[0];

        // replace top item and remove last item
        data[0] = data[lastIndex];
        data.RemoveAt(lastIndex);
        lastIndex--;

        // Sort queue until priorities are in order
        while (true)
        {
            int childIndex = parentIndex * 2 + 1;

            // End of the list
            if (childIndex > lastIndex)
            {
                break;
            }

            int rightChild = childIndex + 1;

            // Right child has a greater priority than the current childIndex
            if (rightChild <= lastIndex && data[rightChild].CompareTo(data[childIndex]) < 0)
            {
                childIndex = rightChild;
            }

            // Already in the correct sorting order
            if (data[parentIndex].CompareTo(data[childIndex]) <= 0)
            {
                break;
            }

            // Resort them
            T tmp = data[parentIndex];
            data[parentIndex] = data[childIndex];
            data[childIndex] = tmp;

            // Update the parent
            parentIndex = childIndex;
        }

        return frontItem;
    }

    /// <summary>
    /// Returns the first item on the list without any sorting
    /// </summary>
    /// <returns></returns>
    public T Peek()
    {
        return data[0];
    }

    /// <summary>
    /// Returns true if the given item is in the list
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(T item)
    {
        return data.Contains(item);
    }

    /// <summary>
    /// Converts priority queue into a generic list
    /// </summary>
    /// <returns></returns>
    public List<T> ToList()
    {
        return data;
    }
}
