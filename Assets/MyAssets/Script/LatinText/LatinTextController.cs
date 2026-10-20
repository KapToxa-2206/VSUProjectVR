using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class LatinTextController : MonoBehaviour
{
    private bool isVisible;
    public Button placementButton;
    public TextMeshProUGUI textMeshPro;  
    public ObjectLists objectLists;
    public List<GameObject> menus;
    public Transform playerTransform;
    public XRRayInteractor leftController;
    public XRRayInteractor rightController;
    public XRGrabInteractable kidneys;

    private void Start() {
        if(placementButton != null) {
            isVisible = false;
            placementButton.onClick.AddListener(Activate);
        }
        for(int i = 0; i < objectLists.organs.Count; i++) {
            objectLists.organs[i].hoverEntered.AddListener(HoverEntered);
            objectLists.organs[i].hoverExited.AddListener(HoverExited);
        }
    }

    private void Update()
    {
        if(isVisible) {
            for(int i = 0; i <  objectLists.organs.Count; i++) {
                if(menus[i].activeSelf) {
                    // Получаем вектор, указывающий от объекта к игроку
                    Vector3 directionToPlayer = menus[i].transform.position - playerTransform.position;

                    // Используем LookRotation, чтобы повернуть объект в сторону игрока
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

                    // Применяем поворот к объекту
                    menus[i].transform.rotation = targetRotation;
                }
            }
        }
    }

    private void HoverEntered(HoverEnterEventArgs args)
    {
        //Debug.Log("HoverEnter");
        if(isVisible) {
            XRGrabInteractable interactable = args.interactable.GetComponent<XRGrabInteractable>();
            XRRayInteractor controller = args.interactor.GetComponent<XRRayInteractor>();
            if(controller != null) {
                for(int i = 0; i < objectLists.organs.Count; i++) {
                    if(interactable == objectLists.organs[i]) {
                        menus[i].SetActive(true);
                        if(interactable == kidneys) {
                            menus[i].transform.position = kidneys.attachTransform.position;
                        } else {
                            menus[i].transform.position = interactable.transform.position;
                        }
                        break;
                    }
                }
            }
        }
    }

    private void HoverExited(HoverExitEventArgs args)
    {
        //Debug.Log("HoverExit");
        if(isVisible) {
            XRGrabInteractable interactable = args.interactable.GetComponent<XRGrabInteractable>();
            XRRayInteractor controller = args.interactor.GetComponent<XRRayInteractor>();
            if(controller != null) {
                for(int i = 0; i < objectLists.organs.Count; i++) {
                    if(interactable == objectLists.organs[i]) {
                        menus[i].SetActive(false);
                        break;
                    }
                }
            }
        }
    }

    public void Activate()
    {
        if(isVisible) {
            isVisible = false;
            for(int i = 0; i < menus.Count; i++) {
                menus[i].SetActive(false);
            }
            textMeshPro.text = "Включить";
        } else {
            isVisible = true;
            textMeshPro.text = "Выключить";
        }
    }
}