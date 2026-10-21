using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class EndConstructor : MonoBehaviour
{
    public Button placementButton; // Ссылка на кнопку UI Button
	public XRSocketInteractor platformSocket;
    public ObjectLists objectLists;
    public ColorLists colorLists;
    public TextMeshProUGUI textMeshPro;

    private void Start()
    {
        placementButton.onClick.AddListener(PlaceObject); // Добавляем слушатель для события "onClick" кнопки   
    }

    private System.Collections.IEnumerator DelayedExecution()
    {
        yield return new WaitForSecondsRealtime(0.001f);
        for(int i = 0; i < objectLists.mainSockets.Count; i++) {
             objectLists.mainSockets[i].socketActive = true;
        }
        platformSocket.socketActive = true;
    }

    private void PlaceObject()
    {
        List<XRSocketInteractor> mainSockets = objectLists.mainSockets;
        List<XRGrabInteractable> organs = objectLists.organs;
	    if (platformSocket != null && platformSocket.selectTarget is XRBaseInteractable) // Проверяем, выбран ли объект в сокете
        {
            XRBaseInteractable target = (XRBaseInteractable)platformSocket.selectTarget; // Получаем ссылку на XRBaseInteractable

	        // Удаляем объект из сокета
            platformSocket.socketActive = false;
            target.transform.SetParent(null);
	    }

        for(int i = 0; i < mainSockets.Count; i++) {
            if(mainSockets[i] != null && mainSockets[i].selectTarget is XRBaseInteractable) {
                XRBaseInteractable target = (XRBaseInteractable)mainSockets[i].selectTarget;

                mainSockets[i].socketActive = false;
                target.transform.SetParent(null);
            }
            mainSockets[i].interactionLayers = objectLists.layerMasks[i];
        }
        colorLists.RestoreAll(); // Возвращаем органам изначальный вид

		for(int i = 0; i < mainSockets.Count; i++) {
        	if (mainSockets[i] != null && organs[i] != null)
        	{
	            XRBaseInteractable target = organs[i] as XRBaseInteractable; // Приводим XRGrabInteractable к типу XRBaseInteractable

	            if (target != null)
        	    {
        		    target.transform.position = mainSockets[i].transform.position; // Устанавливаем позицию объекта на позицию сокета
	                target.transform.rotation = mainSockets[i].transform.rotation; // Устанавливаем поворот объекта в соответствии с поворотом сокета

                	target.transform.SetParent(mainSockets[i].transform); // Устанавливаем родителя объекта в сокет XR Socket
				}
        	}
        }
        textMeshPro.text = "0 из " + objectLists.mainSockets.Count.ToString("D1");
        StartCoroutine(DelayedExecution()); 
    }
}