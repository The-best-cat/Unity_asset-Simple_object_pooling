using UnityEngine;

namespace BlackCatPool
{
    public class GoldenCatCoinBehaviour : MonoBehaviour
    {
        private float elaspedTime = 0;
        private Vector3 start, end;
        private SpriteRenderer sr;
        private ObjectPool pool; //a cache of the pool

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            start = transform.localScale;
            end = start * 1.5f;
        }

        private void OnEnable()
        {
            elaspedTime = 0;
        }

        private void Start()
        {            
            pool = gameObject.GetPool(); //Cache the pool, recommended for performance and code cleanness
                                         //Another approach is pool = ObjectPoolManager.Instance.GetPool("golden_cat_coin");
                                         //Do this in Start()
                                         //Avoid constantly calling GetPool()
        }

        private void Update()
        {
            elaspedTime += Time.deltaTime;

            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Lerp(1, 0, elaspedTime / 2));
            transform.localScale = Vector3.Lerp(start, end, elaspedTime / 2);

            if (elaspedTime > 2f)
            {
                elaspedTime = 0;
                pool.Pool(gameObject); //Pool after expired, automatically deactivated when pooling
                                       //Since the pool is cached, you can write shorter code
                                       //But if there are multiple pools cached, make sure you are returning the object to the correct pool                
            }
        }
    }

}