using UnityEngine;

namespace BlackCatPool
{
    public class PooledObject : MonoBehaviour
    {
        public ObjectPool Pool { get; private set; }
        public GameObject OriginalObject => Pool.PooledObject;

        public bool isPooled;

        private void OnDisable()
        {
            if (!isPooled)
            {
                Pool.Pool(gameObject);
            }
        }

        private void OnDestroy()
        {
            Pool.PooledObjectDestroyed(gameObject); 

            ObjectPoolEvents.EventInvoker.OnWillDestroyPooledObject(gameObject, Pool);
        }

        public void Initialise(ObjectPool pool)
        {
            Pool = pool;
            isPooled = false;
        }
    }

}