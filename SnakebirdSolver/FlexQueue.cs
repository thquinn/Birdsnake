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
        private PriorityQueue<State, int> priorityQueue;
        private Queue<State> queue;

        public FlexQueue(bool priority)
        {
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
        }

        public State Dequeue()
        {
            if (priority)
                return priorityQueue.Dequeue().Value;
            else
                return queue.Dequeue();
        }

        public bool IsEmpty()
        {
            if (priority)
                return priorityQueue.Count == 0;
            else
                return queue.Count == 0;
        }
    }
}
