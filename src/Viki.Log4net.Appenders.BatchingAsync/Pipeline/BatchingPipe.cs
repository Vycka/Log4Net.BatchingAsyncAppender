using System.Collections.Generic;

namespace Viki.Log4net.Appenders.BatchingAsync.Pipeline
{
    public class BatchingPipe<T>
    {
        private bool _completed;

        private List<T> _writeOnlyList, _readOnlyList;

        public BatchingPipe()
        {
            _writeOnlyList = new List<T>();
            _readOnlyList = new List<T>();
        }

        public bool Available => !_completed || _readOnlyList.Count != 0 || _writeOnlyList.Count != 0;

        public bool TryLockBatch(out List<T> batch)
        {
            bool result = false;

            if (_writeOnlyList.Count != 0)
            {
                if (_readOnlyList.Count != 0)
                {
                    batch = _readOnlyList;
                    result = true;
                }
                else
                {
                    Flip();
                    batch = null;
                }
            }
            else if (_completed) // WoS == 0, Completed == TRUE
            {
                if (_readOnlyList.Count != 0)
                {
                    batch = _readOnlyList;
                    result = true;
                }
                else
                {
                    batch = null;
                }
            }
            else
            {
                batch = null;
            }

            return result;
        }

        public void ReleaseBatch()
        {
            _readOnlyList.Clear();

            Flip();
        }

        private void Flip()
        {
            var tmp = _readOnlyList;
            _readOnlyList = _writeOnlyList;
            _writeOnlyList = tmp;
        }

        public void Produce(T item)
        {
            _writeOnlyList.Add(item);
        }

        public void Produce(T[] items)
        {
            _writeOnlyList.AddRange(items);
        }

        public void ProducingCompleted()
        {
            _completed = true;
        }
    }
}