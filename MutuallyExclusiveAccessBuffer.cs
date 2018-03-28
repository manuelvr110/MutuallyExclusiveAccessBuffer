using System;
using System.Collections.Generic;
using System.Threading;

namespace SignalingStructs
{
    class MutuallyExclusiveAccessBuffer<T>
    {
        private Queue<T> Container;
        private int currentLenght
        {
            get
            {
                return Container.Count;
            }
        }
        private readonly object insertDataLocker = new object();
        private readonly object extractDataLocker = new object();

        public int MaxLenght { get; }
        public int CurrentLenght
        {
            get
            {
                lock (Container)
                {
                    return currentLenght;
                }
            }
        }

        public event EventHandler<T> InsertedElement;
        public event EventHandler<T> ExtractedElement;

        public MutuallyExclusiveAccessBuffer(int _bufferSize){
            if (_bufferSize <= 0) throw new ArgumentOutOfRangeException("_bufferSize", "The buffer size must be greater than 0");
            MaxLenght = _bufferSize;
            Container = new Queue<T>(MaxLenght);
        }

        public void TryInsert(T _item)
        {
            lock (Container)
            {
                if (currentLenght == MaxLenght) throw new InvalidOperationException("The buffer is full. Could not insert the element.");
                Container.Enqueue(_item);
                notifyOne(extractDataLocker);
            }
        }

        public void ForceInsert(T _item)
        {
            lock (Container)
            {
                if (currentLenght == MaxLenght) Container.Dequeue();
                Container.Enqueue(_item);
                notifyOne(extractDataLocker);
            }
        }

        public void InsertSynchronously(T _item) {
            lock (Container)
            {
                while (currentLenght == MaxLenght) waitOne(insertDataLocker);
                Container.Enqueue(_item);
                notifyOne(extractDataLocker);
            }
        }

        public T TryExtract()
        {
            lock (Container)
            {
                if (currentLenght == 0) throw new InvalidOperationException("The buffer is empty. Could not extract an element.");
                T dequeuedElement = Container.Dequeue();
                notifyOne(insertDataLocker);
                return dequeuedElement;
            }
        }

        public T ForceExtract()
        {
            lock (Container)
            {
                if (currentLenght == 0) return default(T);
                T dequeuedElement = Container.Dequeue();
                notifyOne(insertDataLocker);
                return dequeuedElement;
            }
        }

        public T ExtractSynchronously()
        {
            lock (Container)
            {
                while (currentLenght == 0) waitOne(extractDataLocker);
                T dequeuedElement = Container.Dequeue();
                notifyOne(insertDataLocker);
                return dequeuedElement;
            }
        }

        public void SafeClear()
        {
            lock (Container)
            {
                Container.Clear();
            }
        }

        protected virtual void OnInsertedElement(T _insertedElement)
        {
            InsertedElement?.Invoke(this, _insertedElement);
        }

        protected virtual void OnExtractedElement(T _extractedElement)
        {
            ExtractedElement?.Invoke(this, _extractedElement);
        }

        ~MutuallyExclusiveAccessBuffer()
        {
            Container.Clear();
        }

        private static void waitOne(Object lockerObject)
        {
            lock (lockerObject) { Monitor.Wait(lockerObject); }
        }

        private static void notifyOne(Object lockerObject)
        {
            lock (lockerObject) { Monitor.Pulse(lockerObject); }
        }

        private static void notifyAll(Object lockerObject)
        {
            lock (lockerObject) { Monitor.PulseAll(lockerObject); }
        }
    }
}
