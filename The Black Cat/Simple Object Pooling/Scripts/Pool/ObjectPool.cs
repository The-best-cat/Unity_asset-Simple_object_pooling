using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlackCatPool
{
    public class ObjectPool : MonoBehaviour
    {
        public string Identifier { get; private set; }
        public GameObject PooledObject { get; private set; }
        public int Capacity { get; private set; }
        public bool IsExpandable { get; private set; } = true;
        public bool PersistBetweenScenes { get; private set; }
        public int ActiveCount => Capacity - pool.Count;
        public int PooledCount => pool.Count;
        public bool IsEmpty => pool.Count == 0;

        private int destroyedObject = 0;
        private bool isRemoved = false;        
        private bool isReturningParent = false;        
        private Queue<PooledObject> pool;
        private Queue<Transform> toBeReturned;
        private Dictionary<GameObject, PooledObject> pooledObjectCache;

        private void OnDisable()
        {
            foreach (var i in pooledObjectCache.Keys.ToList())
            {
                Pool(i);
            }
        }

        private void OnDestroy()
        {
            RemovePool();
        }

        public void Create(string identifier, GameObject pooledObject, int capacity, bool isExpandable, bool persistBetweenScene)
        {
            Identifier = identifier;
            PooledObject = pooledObject;
            Capacity = capacity;
            IsExpandable = isExpandable;            
            PersistBetweenScenes = persistBetweenScene;
            pool = new Queue<PooledObject>(capacity);
            toBeReturned = new Queue<Transform>(capacity);
            pooledObjectCache = new Dictionary<GameObject, PooledObject>(capacity);

            transform.localScale = Vector3.one;

            if (Capacity <= 0)
            {
                Capacity = 10;
            }

            if (persistBetweenScene)
            {
                DontDestroyOnLoad(gameObject);
            }

            ObjectPoolEvents.EventInvoker.OnPoolCreated(this);

            for (int i = 0; i < Capacity; i++)
            {
                CreatePoolObject();
            }
        }

        /// <summary>
        /// Obtain a pooled object from this pool. Returns null if the pool is empty and it is unexpandable.
        /// </summary>        
        /// <param name="setActive">Whether the game object should be activated.</param>
        /// <returns>The pooled game object.</returns>
        public GameObject Get(bool setActive = false)
        {
            while (!IsEmpty && pool.Peek() == null)
            {
                pool.Dequeue();                
            }

            for (int i = 0; i < destroyedObject; i++)
            {
                CreatePoolObject();
            }
            destroyedObject = 0;

            if (IsEmpty && IsExpandable)
            {
                for (int i = 0; i < Capacity; i++)
                {                    
                    CreatePoolObject();
                }
                Capacity *= 2;
            }

            GameObject obj = pool.TryDequeue(out var dequeued) ? dequeued.gameObject : null;
            if (obj != null)
            {
                if (pooledObjectCache.TryGetValue(obj, out var po))
                {
                    pooledObjectCache[obj].isPooled = false;
#if UNITY_EDITOR
                    if (ObjectPoolManager.Instance.ShouldOrganiseHierarchy)
                    {
                        obj.transform.parent = null;
                    }
#endif
                    obj.SetActive(setActive);
                }
                else
                {
                    throw new System.Exception($"For some reason, the game object \"{obj.name}\" does not belong to this pool.");
                }

                ObjectPoolEvents.EventInvoker.OnObjectObtained(obj, this);
            }

            return obj;
        }

        /// <summary>
        /// Try to obtain a pooled object from this pool.
        /// </summary>
        /// <param name="setActive">Whether the game object should be activated.</param>
        /// <param name="obj">The pooled game object.</param>
        /// <returns>True if getting a pooled object is successful. Otherwise false.</returns>
        public bool TryGet(bool setActive, out GameObject obj)
        {
            obj = Get(setActive);
            return obj != null;
        }

        /// <summary>
        /// Return a game object to this pool. 
        /// </summary>
        /// <param name="obj">The game object to be pooled.</param>
        /// <exception cref="System.NullReferenceException"></exception>
        /// <exception cref="System.Exception"></exception>
        public void Pool(GameObject obj)
        {
            if (obj == null)
            {
                throw new System.NullReferenceException("Attempted to pool a null game object.");
            }
            if (!pooledObjectCache.TryGetValue(obj, out var cache))
            {
                if (obj.TryGetPool(out var pool))
                {
                    if (pool != null)
                    {
                        Debug.LogWarning($"Game object \"{obj.name}\" doesn't belong to the {gameObject.name}. It is automatically returned to the correct one.");
                        pool.Pool(obj);
                    }
                    else
                    {
                        throw new System.NullReferenceException($"Pool of {obj.name} doesn't exist.");
                    }
                }
                else
                {
                    throw new System.Exception($"Game object \"{obj.name}\" is not a pooled object.");
                }
                return;
            }

            var po = cache;
            if (po.isPooled)
            {
                return;
            }

            po.isPooled = true;
            if (obj.activeSelf)
            {
                obj.SetActive(false);
            }
            pool.Enqueue(po);

#if UNITY_EDITOR
            if (ObjectPoolManager.Instance.ShouldOrganiseHierarchy)
            {
                toBeReturned.Enqueue(obj.transform);
                if (!isReturningParent)
                {
                    isReturningParent = true;
                    StartCoroutine(ReturnParent());
                }
            }
#endif

            ObjectPoolEvents.EventInvoker.OnObjectPooled(obj, this);
        }

#if UNITY_EDITOR
        private IEnumerator ReturnParent()
        {
            yield return new WaitForSecondsRealtime(0.1f);
            while (toBeReturned.Count > 0)
            {                
                var obj = toBeReturned.Dequeue();
                if (!obj.gameObject.activeSelf)
                {
                    obj.SetParent(transform, true);
                }
            }
            isReturningParent = false;
        }
#endif

        /// <summary>
        /// Obtain multiple pooled objects from this pool. Only returns the remaining pooled objects if the amount is more than the number of pooled objects in this pool and the pool is unexpandable.
        /// </summary>
        /// <param name="amount">The number of objects to be obtained.</param>        
        /// <param name="setActive">Whether the game object should be activated.</param>
        /// <returns>The list containing the pooled objects.</returns>
        public List<GameObject> GetMultiple(int amount, bool setActive = false)
        {
            var list = new List<GameObject>(amount);
            if (amount > pool.Count && !IsExpandable)
            {
                amount = pool.Count;
            }

            for (int i = 0; i < amount; i++)
            {
                list.Add(Get(setActive));
            }
            return list;
        }

        /// <summary>
        /// Remove this pool.
        /// </summary>
        public void RemovePool()
        {
            if (!isRemoved)
            {                
                isRemoved = true;
                while (pool.TryDequeue(out var po))
                {                    
                    if (po != null)
                    {                        
                        Destroy(po.gameObject);
                    }                    
                }
                Destroy(gameObject);                

                ObjectPoolEvents.EventInvoker.OnWillDestroyPool(this);
            }            
        }

        private void CreatePoolObject()
        {            
            GameObject obj = Instantiate(PooledObject);
            var po = obj.TryGetComponent<PooledObject>(out var pooledObject) ? pooledObject : obj.AddComponent<PooledObject>();
            po.Initialise(this);
            pooledObjectCache.Add(obj, po);
            Pool(obj);

            if (PersistBetweenScenes)
            {
                DontDestroyOnLoad(obj);
            }

            ObjectPoolEvents.EventInvoker.OnPooledObjectCreated(obj, this);
        }

        public void PooledObjectDestroyed(GameObject obj)
        {                        
            if (!isRemoved)
            {
                destroyedObject++;
                pooledObjectCache.Remove(obj);
                Debug.LogWarning("Pooled objects are supposed to only be returned and not destroyed. Please make sure destroying a pooled object is intended.");
            }
        }
    }
}