public interface IHeapItem<T> : System.IComparable<T>
{
    int HeapIndex { get; set; }
}
