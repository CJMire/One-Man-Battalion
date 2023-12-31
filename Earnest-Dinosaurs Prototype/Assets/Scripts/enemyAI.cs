using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class enemyAI : MonoBehaviour, IDamage
{
    [Header("----- Enemy's Components ------")]
    [SerializeField] Renderer[] enemyModelArray;
    [SerializeField] NavMeshAgent navAgent;
    [SerializeField] Transform shootPosition;
    [SerializeField] Transform headPos;
    [SerializeField] Transform torsoPos;
    [SerializeField] Animator anim;
    [SerializeField] Collider damageCol;
    [SerializeField] AudioSource aud;
    [SerializeField] ParticleSystem damageParticle;
    [SerializeField] ParticleSystem lightingParticle;

    [Header("----- Enemy's Stats ------")]
    [SerializeField] int HP;
    [SerializeField] int maxHP;
    [SerializeField] int facingSpeed;
    [SerializeField] float damageDuration;
    [SerializeField] float knockbackForce;
    [SerializeField] int viewCone;
    [SerializeField] int shootCone;

    [Header("----- Enemy gun's Stats ------")]
    [SerializeField] GameObject bulletObject;
    [SerializeField] float shootingRate;

    [Header("----- Enemy barrier's Stats ------")]
    [SerializeField] GameObject barrierObject;
    [SerializeField] ParticleSystem barrierParticle;
    [SerializeField] Renderer barrierRenderer;
    [SerializeField] AudioClip barrierDamage;
    [SerializeField] AudioClip barrierDestroy;
    [SerializeField] int barrierHP;

    [Header("----- Drone's Exlusive Component ------")]
    [SerializeField] ParticleSystem droneDeathParticle; 

    [Header("----- Enemy Loot------")]
    [SerializeField] GameObject medkitObject;
    [Range(1,100)][SerializeField] float medkitDropRate;

    [Header("----- Enemy Sound------")]
    [SerializeField] AudioClip[] hurtSound;
    [SerializeField] AudioClip[] deadSound;
    [Range(0, 1)][SerializeField] float enemyVol;

    Color[] modelOrigColor;
    Color barrierOrigColor;

    Vector3 targetDirection;
    bool isShooting;
    bool isDead;
    bool playerInShootingRange;
    float angleToPlayer;
    float stoppingDisOrig;
    int currentLevel;
    Vector3 startingPos;


    void Start()
    {
        //Store different model parts information 
        modelOrigColor = new Color[enemyModelArray.Length];

        for (int i = 0; i < enemyModelArray.Length; i++)
        {
            modelOrigColor[i] = enemyModelArray[i].material.color;
        }

        barrierOrigColor = barrierRenderer.material.color;

        startingPos = transform.position;
        stoppingDisOrig = navAgent.stoppingDistance;
        isDead = false;
        currentLevel = gameManager.instance.GetCurrentLevel();
        gameManager.instance.updateEnemyCount(1);
    }

    // Update is called once per frame
    void Update()
    {
        //If agent is not on then don't do anything
        if (navAgent.isActiveAndEnabled && !isDead)
        {
            //Set the model animation speed along with its navAgent normalized velocity 
            anim.SetFloat("Speed", navAgent.velocity.normalized.magnitude);

            //Player inside the sphere but not see the player 
            if (!canSeePlayer())
            {
                seek();
            }

            seek();
        }
    }

    bool canSeePlayer()
    {
        //Get player direction
        targetDirection = gameManager.instance.player.transform.position - headPos.position;

        //Get angle to the player except y-axis
        angleToPlayer = Vector3.Angle(new Vector3(targetDirection.x, 0, targetDirection.z), transform.forward);

        //Raycast checking 
        RaycastHit hit;

        if (Physics.Raycast(headPos.position, targetDirection, out hit))
        {
            //If the player is within the view cone 
            if (hit.collider.CompareTag("Player") && angleToPlayer <= viewCone)
            {
                navAgent.stoppingDistance = stoppingDisOrig;

                //Shoot the player within the shoot cone 
                if (angleToPlayer <= shootCone && !isShooting && playerInShootingRange)
                {
                    StartCoroutine(shootTarget());
                }

                //Do this when player is in stopping distance 
                if (navAgent.remainingDistance < navAgent.stoppingDistance)
                {
                    faceTarget();
                }

                //Need to stop setting destination when enemy is dead, might find better way to implement this. 
                if (!isDead)
                {
                    //Set the target position as destination 
                    navAgent.SetDestination(gameManager.instance.player.transform.position);
                }

                return true;
            }
        }

        navAgent.stoppingDistance = 0.0f;

        return false;
    }

    void seek()
    {
        if(!isDead)
        {
            //Always go To Player
            faceTarget();
            navAgent.SetDestination(gameManager.instance.player.transform.position);
        }
    }

    void faceTarget()
    {
        //Get rotation to the target 
        Quaternion faceRotation = Quaternion.LookRotation(targetDirection);

        //Rotate to the target using lerp with set up speed
        transform.rotation = Quaternion.Lerp(transform.rotation, faceRotation, Time.deltaTime * facingSpeed);
    }

    public void createShotgunBullet()
    {
        //Create a 6 bullets for shotgun bullet 
        for(int i = 0; i < 6; i++)
        {
            Instantiate(bulletObject, shootPosition.position, transform.rotation);
        }
    }

    public void createBullet()
    {
        //Create a bullet at shooting position and current rotation 
        Instantiate(bulletObject, shootPosition.position, transform.rotation);
    }

    IEnumerator shootTarget()
    {
        isShooting = true;

        anim.SetTrigger("Shoot");

        //Shooting rate 
        yield return new WaitForSeconds(shootingRate);

        isShooting = false;
    }

    public void takeDamage(int damageAmount)
    {
        if(damageAmount > 100)
        {
            barrierObject.SetActive(false);

            int randomSound = UnityEngine.Random.Range(0, deadSound.Length);
            aud.PlayOneShot(deadSound[randomSound], enemyVol);

            //Spawn medkit within drop rate, set isDead and destroy gameObject 
            DropSomething(); //Drops one puick-up for player use

            isDead = true;
            navAgent.enabled = false;
            anim.SetBool("Dead", true);

            //turns off enemy damage colliders when dead
            damageCol.enabled = false;

            //Destroy(gameObject);
            gameManager.instance.updateEnemyCount(-1);

            return;
        }

        if(barrierHP <= 0)
        {
            HP -= damageAmount;

            //Model damage red flash 
            StartCoroutine(damageFeedback());

            //HP is zero then destroy the enemy 
            if (HP <= 0)
            {
                //Spawn medkit within drop rate, set isDead and destroy gameObject 
                int randomSound = UnityEngine.Random.Range(0, deadSound.Length);
                aud.PlayOneShot(deadSound[randomSound], enemyVol);

                DropSomething(); //Drops one puick-up for player use

                isDead = true;
                navAgent.enabled = false;
                anim.SetBool("Dead", true);

                if (lightingParticle != null)
                {
                    Instantiate(lightingParticle, torsoPos.position, lightingParticle.transform.rotation);
                }

                //turns off enemy damage colliders when dead
                damageCol.enabled = false;

                //Destroy(gameObject);
                gameManager.instance.updateEnemyCount(-1);
            }

            else
            {

                //Play damage animation
                anim.SetTrigger("Damage");

                int randomSound = UnityEngine.Random.Range(0, hurtSound.Length);
                aud.PlayOneShot(hurtSound[randomSound], enemyVol);

                //If take damage,then chase the player 
                if (!isDead)
                {
                    navAgent.SetDestination(gameManager.instance.player.transform.position);
                }

                knockback();
            }
        }

        else
        {
            barrierHP -= damageAmount;
            aud.PlayOneShot(barrierDamage, enemyVol);
            StartCoroutine(barrierFeedback());

            if (barrierHP <= 0)
            {
                aud.PlayOneShot(barrierDestroy, enemyVol);
                barrierObject.SetActive(false);
            }
        }
    }

    // Allow for taking in health up to a maximum
    public void heal(int amount)
    {
        HP += amount;
        if (HP > maxHP) HP = maxHP;
    }

    IEnumerator damageFeedback()
    {
        for (int i = 0; i < enemyModelArray.Length; i++)
        {
            enemyModelArray[i].material.color = Color.red;
        }

        Instantiate(damageParticle, torsoPos.position, transform.rotation);

        yield return new WaitForSeconds(damageDuration);

        for (int i = 0; i < enemyModelArray.Length; i++)
        {
            enemyModelArray[i].material.color = modelOrigColor[i];
        }
    }

    IEnumerator barrierFeedback()
    {
        float alpha = barrierOrigColor.a;

        barrierRenderer.material.color = new Color(1.0f, 0.0f, 0.0f, alpha);

        if (barrierParticle != null)
        {
            Instantiate(barrierParticle, torsoPos.position, barrierParticle.transform.rotation);
        }

        yield return new WaitForSeconds(damageDuration);

        barrierRenderer.material.color = barrierOrigColor;
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerInShootingRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInShootingRange = false;
            navAgent.stoppingDistance = 0.0f;
        }
    }

    void knockback()
    {
        //Set velocity to opposite of facing target direction and multiply by force 
        navAgent.velocity = (targetDirection * -1.0f).normalized * knockbackForce;

        //Set angular speed to zero (Enemy face not turning when knocked back)
        navAgent.angularSpeed = 0;
    }

    //Made from Chayathorn's Medkitdrop() method & Cameron's SpeedPickupDrop() and InvincibilityPickupDrop() methods
    //invincibility should be the hardest to get & damage should be 2nd hardest
    //speed should be the 3rd hardest & the medkit should be the easiest
    //However, enemies should only drop 1 thing at time
    //I have set the drop-rates with changable ranges for testing
    void DropSomething()
    {
        float drop = UnityEngine.Random.Range(1, 100);

        if(drop <= medkitDropRate)
        {
            Instantiate(medkitObject, new Vector3(transform.position.x, transform.position.y + 1, transform.position.z), transform.rotation);
        }
    }

    //Destroys gameObject after set amount of time
    IEnumerator OnDeath()
    {
        yield return new WaitForSeconds(1.0f);

        if(currentLevel == 2 && transform.position.y > 0.5f)
        {
            Instantiate(droneDeathParticle, transform.position, transform.rotation);
            Destroy(gameObject);
        }

        else
        {
            //How far the enemy will sink 
            float distance = transform.position.y - 2.0f;

            //Sinking down until the enemy it reaches distance in y-position
            while (transform.position.y >= distance)
            {
                transform.position = transform.position + (Vector3.up * -1.0f) * Time.deltaTime;

                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            Destroy(gameObject);
        }
        
    }

    //Dead behavior for drone 
    IEnumerator OnDroneDeath()
    {
        //The delay splash in the game is because the animation is playing, not the code here. 

        yield return new WaitForSeconds(0.3f);

        if(droneDeathParticle != null)
        {
            Instantiate(droneDeathParticle, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }

    #region Getters and Setters

    public bool GetIsDead()
    {
        return isDead;
    }

    public void SetBarrierHP(int barrierHPAmount)
    {
        barrierHP = barrierHPAmount;

        //Activate barrier if it's more than zero 
        if (barrierHP <= 0)
        {
            barrierObject.SetActive(false);
        }

        else
        {
            barrierObject.SetActive(true);
        }
    }

    #endregion
}
