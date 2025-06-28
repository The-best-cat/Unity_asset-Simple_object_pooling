using UnityEngine;

namespace BlackCatPool
{
    public class Demo : MonoBehaviour
    {
        private float elapsedTime = 0;
        private ObjectPool goldenCoinPool; //The cache of the pool

        private void Start()
        {
            //Cache the pool when creating it
            //This pools a prefab loaded from the Resources folder, this is unnecessary since you can just register it in the ObjectPoolManager in the inspector
            //This is just a code example so you know you can do this
            goldenCoinPool = ObjectPoolManager.Instance.CreateNewPool("golden_cat_coin", Resources.Load<GameObject>("golden_cat_coin"), 10, true, false);

            //If you have already registered a pool and you wish to create it manually by code, you can call
            //ObjectPoolManager.Instance.CreateRegisteredPool(your_identifier);
        }

        private void InitCoin(GameObject coin)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            coin.transform.position = mousePos;
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                elapsedTime += Time.deltaTime;
                while (elapsedTime >= 0.1f)
                {
                    elapsedTime -= 0.1f;

                    //Get a pooled coin directly from the cached pool
                    GameObject coin = goldenCoinPool.Get(true);
                    InitCoin(coin);
                }
                
            }
            if (Input.GetMouseButtonDown(1))
            {
                //This pool is registered in the ObjectPoolManager in the inspector
                //This pool isn't cached, so the pooled coin needs to be obtained by the manager, providing the pool identifier
                GameObject coin = ObjectPoolManager.Instance.GetObject("silver_cat_coin", true);

                if (coin != null) //Requires a null check because silver coin pool is unexpandable
                                  //An unexpandable pool returns null if it is already empty when you want to obtain an object from it
                {
                    InitCoin(coin);
                }

                //another approach is this:
                //if (ObjectPoolManager.Instance.TryGetObject("silver_cat_coin", true, out var coin))
                //{
                //    InitCoin(coin);
                //}
            }
        }
    }

}