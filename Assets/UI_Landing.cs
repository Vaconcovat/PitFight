using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Landing : MonoBehaviour
{

    public void Quit()
    {
        Application.Quit();
    }

    public void ChangeScene(string scene) {
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }
}
