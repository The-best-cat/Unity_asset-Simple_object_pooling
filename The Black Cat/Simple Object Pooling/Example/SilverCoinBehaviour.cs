using UnityEngine;

namespace BlackCatPool
{
    public class SilverCoinBehaviour : MonoBehaviour
    {
        private float elaspedTime = 0;
        private Vector3 start, end;
        private SpriteRenderer sr;

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

        private void Update()
        {
            elaspedTime += Time.deltaTime;

            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, Mathf.Lerp(1, 0, elaspedTime / 2));
            transform.localScale = Vector3.Lerp(start, end, elaspedTime / 2);

            if (elaspedTime > 2f)
            {
                elaspedTime = 0;
                ObjectPoolManager.Instance.PoolObject(gameObject); //Pool after expired, automatically deactivated when pooling
                                                                   //Unlike golden cat coin, this didn't cache the pool, resulting in longer code
                                                                   //But this guarantees the object is returned to the correct pool 
            }
        }
    }

}