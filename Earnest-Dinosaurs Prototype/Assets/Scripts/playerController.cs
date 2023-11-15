using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class playerController : MonoBehaviour, IDamage
{
    [Header("----- Components -----")]
    [SerializeField] CharacterController characterController;

    [Header("----- Player Stats -----")]
    [SerializeField] int HP;
    private int maxHP;
    [SerializeField] float playerSpeed;
    [SerializeField] float playerJumpHeight;
    [SerializeField] int playerJumpMax;
    [SerializeField] float gravityStrength;
    [Range(1, 10)][SerializeField] float sprintMod;

    [Header("----- Player Gun Stats -----")]
    [SerializeField] gunStats starterGun;
    [SerializeField] List<gunStats> gunList = new List<gunStats>();
    [SerializeField] GameObject gunModel;

    [SerializeField] int shootDamage;
    [SerializeField] float shootDistance;
    [SerializeField] float shootRate;
    [SerializeField] float reloadTime;
    
    [SerializeField] GameObject bulletObject;

    private Vector3 move;
    private Vector3 playerVelocity;
    private bool playerIsGrounded;
    private int jumpTimes;
    private bool isShooting;
    private bool isReloading;
    private bool isSprinting;
    int selectedGun;


    void Start()
    {
        //Sets the starter gun
        getGunStats(starterGun);
        //sets maxHP
        maxHP = HP;
        //spawns player in current level
        spawnPlayer();
    }

    void Update()
    {
        if (!gameManager.instance.GetIsPaused())
        {
            Movement();
        }
    }

    void Movement()
    {
        sprint();

        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * shootDistance, Color.red);

        //Checks if the player can shoot
        if (Input.GetButton("Shoot") && !isShooting && !isReloading && gunList[selectedGun].ammoCur > 0)
        {
            StartCoroutine(Shoot());
        }

        playerIsGrounded = characterController.isGrounded;
        if (playerIsGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0;
            jumpTimes = 0;
        }

        move = Input.GetAxis("Horizontal") * transform.right +
               Input.GetAxis("Vertical") * transform.forward;
        characterController.Move(move * Time.deltaTime * playerSpeed);

        if (Input.GetButtonDown("Jump") && jumpTimes < playerJumpMax)
        {
            playerVelocity.y = playerJumpHeight;
            jumpTimes++;
        }
        playerVelocity.y += gravityStrength * Time.deltaTime;
        characterController.Move(playerVelocity * Time.deltaTime);

        //Checks if the player can reload
        if (Input.GetButtonDown("Reload") && !isShooting && !isReloading && gunList[selectedGun].ammoCur < gunList[selectedGun].ammoMax)
        {
            StartCoroutine(gameManager.instance.Reload());
        }
    }

    void sprint()
    {
        if (Input.GetButtonDown("Sprint"))
        {
            isSprinting = true;
            playerSpeed *= sprintMod;
        }
        else if (Input.GetButtonUp("Sprint"))
        {
            isSprinting = false;
            playerSpeed /= sprintMod;
        }
    }

    IEnumerator Shoot()
    {
        isShooting = true;
        gunList[selectedGun].ammoCur--;
        gameManager.instance.updateHUD();

        RaycastHit hit;
        //for use when RaycastHit does hit an enemy
        float offset = 0;
        if (Physics.Raycast(Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f)), out hit, shootDistance))
        {
            IDamage damageable = hit.collider.GetComponent<IDamage>();

            if (hit.transform != transform && damageable != null)
            {
                damageable.takeDamage(shootDamage);

                //since the hitmarker will be shown for x amount of time, we must offset the next time the player can shoot
                offset = gameManager.instance.getHitMarkerRate();

                //On hit, shows the hitmarker for the hitMarkerRate duration in gameManager
                gameManager.instance.GetHitMarker().gameObject.SetActive(true);
                yield return new WaitForSeconds(offset);
                gameManager.instance.GetHitMarker().gameObject.SetActive(false);
            }

        }
        //If the raycastHit doesn't hit, there's no subtraction and shootrate remains constant
        //if it does, there's a slightly longer wait to shoot again. this ensures there's no loss in time
        yield return new WaitForSeconds(shootRate - offset);
        isShooting = false;
    }

    public void takeDamage(int damageAmount)
    {
        //Updates HP value and HUD
        HP -= damageAmount;
        StartCoroutine(gameManager.instance.playerHurtFlash());
        //makes sure no HP is negative & calls lose screen
        if (HP <= 0)
        {
            HP = 0;
            gameManager.instance.updateHUD();
            gameManager.instance.OnDeath();
            return;
        }
        gameManager.instance.updateHUD();
    }

    public void healPlayer(int amount)
    {
        //Heal the player 
        HP += amount;
        if (HP > maxHP)
            HP = maxHP;
        gameManager.instance.updateHUD();
    }

    public void ReloadSuccess()
    {
        gunList[selectedGun].ammoCur = gunList[selectedGun].ammoMax;
        isReloading = false;
        gameManager.instance.updateHUD();
    }

    public void spawnPlayer()
    {
        characterController.enabled = false;
        transform.position = gameManager.instance.GetSpawnPos().position;
        transform.rotation = gameManager.instance.GetSpawnPos().rotation;
        characterController.enabled = true;
        HP = maxHP;
        isShooting = false;
        isReloading = false;
        gameManager.instance.GetReloadIcon().SetActive(false);
        ReloadSuccess();
    }

    void changeGun()
    {
        shootDamage = gunList[selectedGun].shootDamage;
        shootDistance = gunList[selectedGun].shootDist;
        shootRate = gunList[selectedGun].shootRate;
        reloadTime = gunList[selectedGun].reloadTime;

        gunModel.GetComponent<MeshFilter>().sharedMesh = gunList[selectedGun].model.GetComponent<MeshFilter>().sharedMesh;
        gunModel.GetComponent<MeshRenderer>().sharedMaterial = gunList[selectedGun].model.GetComponent<MeshRenderer>().sharedMaterial;

        isShooting = false;
    }

    #region Getters & Setters
    public int getPlayerMaxHP()
    {
        return maxHP;
    }

    public int getPlayerCurrentHP()
    {
        return HP;
    }

    public float getPlayerCurrentSpeed()
    {
        return playerSpeed;
    }

    public int getPlayerMaxAmmo()
    {
        return gunList[selectedGun].ammoMax;
    }

    public int getPlayerCurrentAmmo()
    {
        return gunList[selectedGun].ammoCur;
    }

    public bool getIsShooting()
    {
        return isShooting;
    }

    public void SetIsShooting(bool isShooting)
    {
        this.isShooting = isShooting;
    }

    public bool GetIsReloading()
    {
        return isReloading;
    }

    public void SetIsReloading(bool isReloading)
    {
        this.isReloading = isReloading;
    }

    public float GetReloadTime()
    {
        return reloadTime;
    }

    public void getGunStats(gunStats gun)
    {
        gunList.Add(gun);

        shootDamage = gun.shootDamage;
        shootDistance = gun.shootDist;
        shootRate = gun.shootRate;
        reloadTime = gun.reloadTime;

        gunModel.GetComponent<MeshFilter>().sharedMesh = gun.model.GetComponent<MeshFilter>().sharedMesh;
        gunModel.GetComponent<MeshRenderer>().sharedMaterial = gun.model.GetComponent<MeshRenderer>().sharedMaterial;

        selectedGun = gunList.Count - 1;
    }
    #endregion
}