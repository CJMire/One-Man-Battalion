using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneChange : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        gameManager.instance.SavePlayerData();
        if (other.CompareTag("Player") && SceneManager.GetActiveScene().name  == "Level 1")
        {
            gameManager.instance.switchSceneAsync("Level 2");
        }
        else if(other.CompareTag("Player") && SceneManager.GetActiveScene().name == "Level 2")
        {
            gameManager.instance.switchSceneAsync("Level 3");
        }
    }


}
