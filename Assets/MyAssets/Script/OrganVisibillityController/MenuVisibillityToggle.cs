using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
public class MenuVisibillityToggle : MonoBehaviour
{
    public GameObject menu;
    public Button button;                            // Кнопка переключения
    private bool isVisible = false;              // Текущее состояние

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(Toggle);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Toggle);
    }

    private void Toggle()
    {
        if(isVisible)
        {
            menu.SetActive(false);
            isVisible = false;
        } else
        {
            menu.SetActive(true);
            isVisible = true;
        }
    }
}
