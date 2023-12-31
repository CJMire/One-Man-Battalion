using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class invincibility : MonoBehaviour
{
    [Header("---- Pickup Stats ----")]
    [SerializeField] int rotationSpeed;
    [SerializeField] int pickupGroundDuration;
    [SerializeField] int pickupDuration;

    public GameObject[] bulletArray;

    public bool isInvincible;
    // Start is called before the first frame update
    void Start()
    {
        isInvincible = false;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0.0f, 20.0f, 0.0f) * Time.deltaTime * rotationSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
        {
            return;
        }

        if (other.gameObject.CompareTag("Player"))
        {
            StartCoroutine(invincible());

        }
    }

    IEnumerator invincible()
    {
        isInvincible = true;

        for(int i = 0; i < bulletArray.Length; i++)
        {
            bulletArray[i].GetComponent<SphereCollider>().enabled = false;
        }

        transform.position = new Vector3(0, -10, 0);

        yield return new WaitForSeconds(pickupDuration);

        for(int i = 0; i < bulletArray.Length; i++)
        {
            bulletArray[i].GetComponent<SphereCollider>().enabled = true;
        }

        isInvincible = false;

        Destroy(gameObject);
    }
}
