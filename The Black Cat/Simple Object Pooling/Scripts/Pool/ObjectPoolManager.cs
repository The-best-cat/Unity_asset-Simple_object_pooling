using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlackCatPool
{   
    [Serializable]
    public class PoolObjectData
    {
        [SerializeField] private string identifier;
        [SerializeField] private GameObject poolObject;
        [SerializeField] private int capacity = 10;
        [SerializeField] private bool isExpandable;
        [SerializeField] private CreateTime createTime;
        [SerializeField] private string sceneName;
        [SerializeField] private bool persistBetweenScenes = false;

        [SerializeField] private bool foldoutExpanded = true;

        public string Identifier => identifier;
        public GameObject PoolObject => poolObject;
        public int Capacity => capacity;
        public bool IsExpandable => isExpandable;
        public CreateTime PoolCreateTime => createTime;
        public string SceneName => sceneName;
        public bool PersistBetweenScenes => persistBetweenScenes;

        public enum CreateTime
        {
            ManagerCreated,
            SceneLoaded,
            Custom
        }

#if UNITY_EDITOR
        private void SoItDoesntGiveMeAWarning()
        {
            Debug.Log(foldoutExpanded);
        }
#endif
    }

    public class ObjectPoolManager : MonoBehaviour
    {
        [SerializeField] private bool listExpanded = true;

        [SerializeField] private bool persistBetweenScenes = true;
        [SerializeField] private bool hierarchyOrganisation = true;
        [SerializeField] private List<PoolObjectData> poolObjects = new List<PoolObjectData>();

#if UNITY_EDITOR
        public bool ShouldOrganiseHierarchy => hierarchyOrganisation;
#endif

        private Dictionary<string, List<PoolObjectData>> createPoolInScene = new Dictionary<string, List<PoolObjectData>>();
        private Dictionary<string, PoolObjectData> customCreatePool = new Dictionary<string, PoolObjectData>();
        private Dictionary<string, ObjectPool> pools;
        private Dictionary<GameObject, ObjectPool> activeObjects;
        private HashSet<GameObject> pooledObjects = new HashSet<GameObject>();

        public static ObjectPoolManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (persistBetweenScenes)
                {
                    DontDestroyOnLoad(gameObject);
                }

                pools = new Dictionary<string, ObjectPool>(poolObjects.Count);
                pooledObjects = new HashSet<GameObject>(poolObjects.Count);

                int currentMaxCap = 0;
                foreach (var poolData in poolObjects)
                {
                    if (poolData.PoolCreateTime == PoolObjectData.CreateTime.ManagerCreated)
                    {
                        CreateNewPool(poolData.Identifier, poolData.PoolObject, poolData.Capacity, poolData.IsExpandable, poolData.PersistBetweenScenes);
                    }
                    else if (poolData.PoolCreateTime == PoolObjectData.CreateTime.SceneLoaded)
                    {
                        if (createPoolInScene.TryGetValue(poolData.SceneName, out var list))
                        {
                            bool skip = false;
                            foreach (var data in list)
                            {
                                if (data.Identifier == poolData.Identifier)
                                {
                                    Debug.LogWarning($"A pool identified by the \"{poolData.Identifier}\" is already registered.");
                                    skip = true;
                                    break;
                                }
                            }
                            if (skip)
                            {
                                continue;
                            }
                            list.Add(poolData);
                        }
                        else
                        {
                            createPoolInScene.Add(poolData.SceneName, new List<PoolObjectData> { poolData });
                        }
                    }
                    else
                    {
                        if (customCreatePool.ContainsKey(poolData.Identifier))
                        {
                            Debug.LogWarning($"A pool identified by the \"{poolData.Identifier}\" is already registered.");
                        }
                        else
                        {
                            customCreatePool.Add(poolData.Identifier, poolData);
                        }
                    }
                    currentMaxCap += poolData.Capacity;
                }
                activeObjects = new Dictionary<GameObject, ObjectPool>(currentMaxCap);                

                poolObjects.Clear();
                poolObjects = null;                
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            ObjectPoolEvents.onWillDestroyPool += OnWillDestroyPool;
            ObjectPoolEvents.onObjectObtained += OnObjectObtained;
            ObjectPoolEvents.onObjectPooled += OnObjectPooled;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            ObjectPoolEvents.onWillDestroyPool -= OnWillDestroyPool;
            ObjectPoolEvents.onObjectObtained -= OnObjectObtained;  
            ObjectPoolEvents.onObjectPooled -= OnObjectPooled;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// This creates a new object pool.
        /// </summary>
        /// <param name="identifier">The identifier (name) of the object pool.</param>
        /// <param name="poolObject">The game object or prefab to be pooled</param>
        /// <param name="capacity">The initial capacity of the object pool.</param>
        /// <param name="isExpandable">Whether the object pool should expand its capacity.</param>
        /// <param name="repoolOnDisable">Whether the object pool will repool every object obtained from it on disable.</param>
        /// <param name="persistBetweenScenes">Whether the object pool should persist when scene changes.</param>
        /// <returns>Returns the created object pool.</returns>
        /// <exception cref="NullReferenceException">When the object to be pooled is null.</exception>
        public ObjectPool CreateNewPool(string identifier, GameObject poolObject, int capacity = 10, bool isExpandable = true, bool persistBetweenScenes = false)
        {
            if (poolObject == null)
            {
                throw new NullReferenceException("Attempted to create a pool with a null GameObject.");
            }

            if (!TryGetPool(identifier, out var pool))
            {
                if (pooledObjects.Contains(poolObject))
                {
                    foreach (var i in pools)
                    {
                        if (i.Value.PooledObject == poolObject)
                        {
                            Debug.LogWarning($"Game object \"{poolObject.name}\" is already being pooled by \"{i.Key}\". Please make sure pooling the same game object in different pools is intended.");
                        }
                    }
                }
                else
                {
                    pooledObjects.Add(poolObject);
                }

                GameObject container = new GameObject($"Pool of {poolObject.name}");
                ObjectPool newPool = container.AddComponent<ObjectPool>();
                newPool.Create(identifier, poolObject, capacity, isExpandable, persistBetweenScenes);
                pools.Add(identifier, newPool);
                container.transform.localScale = Vector3.one;

                return newPool;
            }
            else
            {
                Debug.LogWarning($"Pool identified by \"{identifier}\" already exists.");
                return pool;
            }
        }

        /// <summary>
        /// This creates a pool that is registered in the inspector and with a custom creation time.
        /// </summary>
        /// <param name="identifier">The pool identifier.</param>
        /// <returns>The created object pool.</returns>
        public ObjectPool CreateRegisteredPool(string identifier)
        {
            if (customCreatePool.TryGetValue(identifier, out var data))
            {
                return CreateNewPool(data.Identifier, data.PoolObject, data.Capacity, data.IsExpandable, data.PersistBetweenScenes);
            }
            Debug.LogWarning($"No pool is registered by the identifier \"{identifier}\".");
            return null;
        }

        /// <summary>
        /// This returns the object pool identified by the identifier you pass in. Returns null if the object pool cannot be found.
        /// </summary>
        /// <param name="identifier">The pool identifier.</param>
        /// <returns>The object pool identified by the identifier you pass in.</returns>
        public ObjectPool GetPool(string identifier)
        {
            if (pools.TryGetValue(identifier, out var pool))
            {
                return pool;
            }
            else
            {
                Debug.LogWarning($"Pool identified by \"{identifier}\" doesn't exist.");
                return null;
            }
        }

        /// <summary>
        /// Try to get the object pool identified by the identifier you pass in.
        /// </summary>
        /// <param name="identifier">The pool identifier.</param>
        /// <param name="pool">The found object pool. Null if the object pool cannot be found.</param>
        /// <returns>True if the object pool is found. Otherwise false.</returns>
        public bool TryGetPool(string identifier, out ObjectPool pool)
        {
            if (pools.TryGetValue(identifier, out var objectPool))
            {
                pool = objectPool;
                return true;
            }
            pool = null;
            return false;
        }

        /// <summary>
        /// Obtain a pooled object from the specific pool. Returns null if the pool cannot be found or if the pool is empty and it is unexpandable.
        /// </summary>
        /// <param name="identifier">The pool identifier.</param>        
        /// <param name="setActive">Whether the game object should be activated.</param>
        /// <returns>The pooled game object.</returns>
        public GameObject GetObject(string identifier, bool setActive = false)
        {
            if (TryGetPool(identifier, out var pool))
            {
                return pool.Get(setActive);
            }
            Debug.LogWarning($"Pool identified by \"{identifier}\" doesn't exist.");
            return null;
        }

        /// <summary>
        /// Try to obtain a pooled object from the specific pool.
        /// </summary>
        /// <param name="identifier">The pool identifier.</param>
        /// <param name="setActive">Whether the game object should be activated.</param>
        /// <param name="obj">The pooled game object.</param>
        /// <returns>True if getting a pooled object is successful. Otherwise false.</returns>
        public bool TryGetObject(string identifier, bool setActive, out GameObject obj)
        {
            obj = GetObject(identifier, setActive);
            return obj != null;
        }

        [Obsolete("PoolObject(string, GameObject) is not recommended. Use PoolObject(GameObject) instead.")]
        /// <summary>
        /// Return a game object to a specific pool. 
        /// </summary>
        /// <param name="identifier">The pool identifier.</param>
        /// <param name="obj">The game object to be pooled.</param>
        public void PoolObject(string identifier, GameObject obj)
        {
            if (pools.TryGetValue(identifier, out var pool))
            {
                pool.Pool(obj);
            }
            else
            {
                Debug.LogWarning($"Pool identified by \"{identifier}\" doesn't exist.");
            }
        }

        /// <summary>
        /// Return a game object to its pool. The pool is found automatically.
        /// </summary>
        /// <param name="obj">The game object to be pooled.</param>
        public void PoolObject(GameObject obj)
        {            
            if (activeObjects.TryGetValue(obj, out var pool))
            {
                pool.Pool(obj);                
            }
            else
            {
                Debug.LogWarning($"The game object \"{obj.name}\" is not a pooled object.");
            }
        }

        /// <summary>
        /// Obtain multiple pooled objects from the specific pool. Returns null if the pool cannot be found. Only returns the remaining pooled objects if the amount is more than the number of pooled objects in this pool and the pool is unexpandable.
        /// </summary>
        /// <param name="identifier">The pool identifier.</param>
        /// <param name="amount">The number of objects to be obtained.</param>        
        /// <param name="setActive">Whether the game object should be activated.</param>
        /// <returns>The list containing the pooled objects.</returns>
        public List<GameObject> GetMultipleObjects(string identifier, int amount, bool setActive = false)
        {
            if (TryGetPool(identifier, out var pool))
            {
                return pool.GetMultiple(amount, setActive);
            }
            else
            {
                Debug.LogWarning($"Pool identified by \"{identifier}\" doesn't exist.");
                return null;
            }
        }

        /// <summary>
        /// Remove an existing pool.
        /// </summary>
        /// <param name="identifier">The pool identifier.</param>
        public void RemovePool(string identifier)
        {
            if (TryGetPool(identifier, out var pool))
            {
                pool.RemovePool();                                
            }
            else
            {
                Debug.LogWarning($"Pool identified by \"{identifier}\" doesn't exist.");
            }
        }

        private void OnWillDestroyPool(ObjectPool pool)
        {
            pools.Remove(pool.Identifier);            
        }

        private void OnObjectObtained(GameObject obj, ObjectPool pool)
        {
            if (activeObjects.TryGetValue(obj, out _))
            {
                throw new Exception($"The game object \"{obj.name}\" is already obtained from the pool, but another same game object is obtained again.");
            }
            else
            {                
                activeObjects.Add(obj, pool);
            }
        }

        private void OnObjectPooled(GameObject obj, ObjectPool pool)
        {
            activeObjects.Remove(obj);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (createPoolInScene.TryGetValue(scene.name, out var list))
            {
                foreach (var data in list)
                {
                    if (!pools.ContainsKey(data.Identifier))
                    {
                        CreateNewPool(data.Identifier, data.PoolObject, data.Capacity, data.IsExpandable, data.PersistBetweenScenes);
                    }
                }
            }
        }

#if UNITY_EDITOR
        public List<KeyValuePair<string, ObjectPool>> GetPools()
        {
            return new List<KeyValuePair<string, ObjectPool>>(pools);
        }

        private void SoItDoesntGiveMeAWarning()
        {
            Debug.Log(listExpanded);
        }
#endif
    }
}