using UnityEngine;

namespace BlackCatPool
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Get the pool this game object belongs to. Returns null if this is not a pooled object or if the pool doesn't exist.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>The pool this game object belongs to.</returns>
        public static ObjectPool GetPool(this GameObject obj)
        {
            if (obj.TryGetComponent<PooledObject>(out var po))
            {
                return po.Pool;
            }
            return null;
        }

        /// <summary>
        /// Try to get the pool this game object belongs to. The object pool is out.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="pool">The object pool this game object belongs to.</param>
        /// <returns>True if this game object is a pooled object and the object pool exists and is found. Otherwise false.</returns>
        public static bool TryGetPool(this GameObject obj, out ObjectPool pool)
        {
            if (obj.TryGetComponent<PooledObject>(out var po))
            {
                pool = po.Pool;
                return true;
            }
            pool = null;
            return false;
        }
    }

}