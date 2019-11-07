using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakebirdSolver
{
    public class FlexQueue<T>
    {
        private bool priority;
        private int capacity;
        private PriorityQueue<State, int> priorityQueue;
        private Queue<State> queue;

        public FlexQueue(bool priority, int capacity = -1)
        {
            this.capacity = capacity;
            this.priority = priority;
            if (priority)
                priorityQueue = new PriorityQueue<State, int>();
            else
                queue = new Queue<State>();
        }

        public void Enqueue(State state)
        {
            if (priority)
                priorityQueue.Enqueue(state, state.Heuristic());
            else
                queue.Enqueue(state);

            // If we're using a priority queue, the back half isn't worth much. Get rid of it.
            if (priority && Count() > capacity)
                Halve();
        }

        public State Dequeue()
        {
            if (priority)
                return priorityQueue.Dequeue().Value;
            else
                return queue.Dequeue();
        }

        public void Halve()
        {
            if (priority)
            {
                int target = capacity / 2;
                PriorityQueue<State, int> newQueue = new PriorityQueue<State, int>();
                while (newQueue.Count < target)
                    newQueue.Enqueue(priorityQueue.Dequeue());
                priorityQueue.Clear();
                priorityQueue = newQueue;
            }
            else
            {
                int target = capacity / 2;
                Queue<State> newQueue = new Queue<State>();
                while (newQueue.Count < target)
                    newQueue.Enqueue(queue.Dequeue());
                queue.Clear();
                queue = newQueue;
            }
        }

        public int Count()
        {
            if (priority)
                return priorityQueue.Count;
            else
                return queue.Count;
        }

        public bool IsEmpty()
        {
            return Count() == 0;
        }
    }
}
