using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class BeginAgain : MonoBehaviour
{
    public Button placementButton; // Ссылка на кнопку UI Button
    public XRSocketInteractor platformSocket;
    public ObjectLists objectLists;
    public ColorLists colorLists;
    public TextMeshProUGUI textMeshPro;
    // public XRController leftController;
    // public XRController rightController;

    private void Start()
    {
        if(placementButton != null) {
            placementButton.onClick.AddListener(TeleportObject); // Добавляем слушатель для события "onClick" кнопки 
        }  
    }

    private System.Collections.IEnumerator DelayedExecution()
    {
        yield return new WaitForSecondsRealtime(0.001f);
        for(int i = 0; i < objectLists.mainSockets.Count; i++) {
            objectLists.mainSockets[i].socketActive = true;
            objectLists.teleportSockets[i].socketActive = true;
        }
        platformSocket.socketActive = true;
        // leftController.socketActive = true;
        // rightController.socketActive = true;
    }

    private System.Collections.IEnumerator DelayedExecutionTeleport()
    {
        yield return new WaitForSecondsRealtime(0.001f);
        for(int i = 0; i < objectLists.mainSockets.Count; i++) {
            objectLists.teleportSockets[i].socketActive = true;
        }
    }

    public void TeleportObject()
    {
        if (platformSocket != null && platformSocket.selectTarget is XRBaseInteractable) // Проверяем, выбран ли объект в сокете
        {
            XRBaseInteractable target = (XRBaseInteractable)platformSocket.selectTarget; // Получаем ссылку на XRBaseInteractable

	        // Удаляем объект из сокета
            platformSocket.socketActive = false;
            target.transform.SetParent(null);
	    }

        // if(leftController != null && leftController.selectTarget is XRBaseInteractable) {
        //     XRBaseInteractable target = (XRBaseInteractable)leftController.selectTarget; // Получаем ссылку на XRBaseInteractable

	    //     // Удаляем объект из сокета
        //     leftController.socketActive = false;
        //     target.transform.SetParent(null);
        // }

        // if(rightController != null && rightController.selectTarget is XRBaseInteractable) {
        //     XRBaseInteractable target = (XRBaseInteractable)rightController.selectTarget; // Получаем ссылку на XRBaseInteractable

	    //     // Удаляем объект из сокета
        //     rightController.socketActive = false;
        //     target.transform.SetParent(null);
        // }

        List<XRSocketInteractor> mainSockets = objectLists.mainSockets;
        List<XRSocketInteractor> teleportSockets = objectLists.teleportSockets;

        for(int i = 0; i < mainSockets.Count; i++) {
            if (mainSockets[i] != null && mainSockets[i].selectTarget is XRBaseInteractable) // Проверяем, выбран ли объект в сокете
            {
                XRBaseInteractable target = (XRBaseInteractable)mainSockets[i].selectTarget;
	            // Удаляем объект из сокета
                mainSockets[i].socketActive = false;
                //target.transform.SetParent(null);
	        }
        }

        for(int i = 0; i < teleportSockets.Count; i++) {
            if (teleportSockets[i] != null && teleportSockets[i].selectTarget is XRBaseInteractable) // Проверяем, выбран ли объект в сокете
            {
                XRBaseInteractable target = (XRBaseInteractable)teleportSockets[i].selectTarget;
	            // Удаляем объект из сокета
                teleportSockets[i].socketActive = false;
                //target.transform.SetParent(null);
	        }
        }
        
        for(int i = 0; i < teleportSockets.Count; i++) 
        {
            XRBaseInteractable target = objectLists.organs[i] as XRBaseInteractable;  // Получаем ссылку на XRBaseInteractable
            colorLists.RestoreOrgan(i); // Возвращаем изначальный вид органа

            if(target != null) {
                target.transform.position = teleportSockets[i].transform.position;
                target.transform.rotation = teleportSockets[i].transform.rotation;
                //target.transform.SetParent(teleportSockets[i].transform);
            }
        }
        textMeshPro.text = "0 из " + objectLists.mainSockets.Count.ToString("D1");
        StartCoroutine(DelayedExecution());
    }
}