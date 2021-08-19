using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Glowwave
{
    class PriorityQueue<T>
    {
        Func<T, T, int> _cmp;

        private List<T> _list = new List<T>();
        public int Count { get { return _list.Count; } }

        public PriorityQueue(Func<T, T, int> cmp)
        {
            _cmp = cmp;
        }

        public void Push(T x)
        {
            _list.Add(x);
            int i = Count - 1;

            while(i > 0) {
                int p = (i - 1) / 2;
                if(_cmp(_list[p], x) <= 0) {
                    break;
                }

                _list[i] = _list[p];
                i = p;
            }

            if(Count > 0) {
                _list[i] = x;
            }
        }

        public T Pop()
        {
            T target = Peek();
            T root = _list[Count - 1];
            _list.RemoveAt(Count - 1);

            int i = 0;
            while(i * 2 + 1 < Count) {
                int a = i * 2 + 1;
                int b = i * 2 + 2;
                int c = b < Count && _cmp(_list[b], _list[a]) < 0 ? b : a;

                if(_cmp(_list[c], root) >= 0) {
                    break;
                }

                _list[i] = _list[c];
                i = c;
            }

            if(Count > 0) _list[i] = root;
            return target;
        }

        public T Peek()
        {
            if(Count == 0) throw new InvalidOperationException("Queue is empty.");
            return _list[0];
        }

        public void Clear()
        {
            _list.Clear();
        }
    }
}
