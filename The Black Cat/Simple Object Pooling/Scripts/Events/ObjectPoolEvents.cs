using System;
using UnityEngine;

namespace BlackCatPool
{
    public class ObjectPoolEvents
    {
        public static event Action<GameObject, ObjectPool> onPooledObjectCreated;
        public static event Action<GameObject, ObjectPool> onWillDestroyPooledObject;
        public static event Action<GameObject, ObjectPool> onObjectObtained;
        public static event Action<GameObject, ObjectPool> onObjectPooled;

        public static event Action<ObjectPool> onPoolCreated;
        public static event Action<ObjectPool> onWillDestroyPool;

        public static class EventInvoker
        {
            public static void OnPooledObjectCreated(GameObject obj, ObjectPool pool) => onPooledObjectCreated?.Invoke(obj, pool);
            public static void OnWillDestroyPooledObject(GameObject obj, ObjectPool pool) => onWillDestroyPooledObject?.Invoke(obj, pool);
            public static void OnObjectObtained(GameObject obj, ObjectPool pool) => onObjectObtained?.Invoke(obj, pool);
            public static void OnObjectPooled(GameObject obj, ObjectPool pool) => onObjectPooled?.Invoke(obj, pool);
            public static void OnPoolCreated(ObjectPool pool) => onPoolCreated?.Invoke(pool);
            public static void OnWillDestroyPool(ObjectPool pool) => onWillDestroyPool?.Invoke(pool);
        }
    }

}