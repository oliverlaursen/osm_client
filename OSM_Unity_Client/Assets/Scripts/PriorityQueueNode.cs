using Priority_Queue;
public class PriorityQueueNode : FastPriorityQueueNode
    {
        public long Id { get; private set; }

        public PriorityQueueNode(long id)
        {
            Id = id;
        }
    }