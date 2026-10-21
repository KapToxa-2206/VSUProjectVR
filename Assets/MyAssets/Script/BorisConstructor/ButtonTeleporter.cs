using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;

public class ButtonTeleporter : MonoBehaviour
{
    public Button placementButton; // Ссылка на кнопку UI Button
    public XRSocketInteractor platformSocket;
    public ObjectLists objectLists;
    public InteractionLayerMask everythingMask;

    private void Start()
    {
        if(placementButton != null) {
            placementButton.onClick.AddListener(TeleportObject); // Добавляем слушатель для события "onClick" кнопки 
        }  
    }

    /// <summary>
    /// Старт конструктора: органы переезжают из основных сокетов в телепорт-сокеты.
    /// Перемещение выполняется синхронно через API XR Interaction Toolkit,
    /// поэтому отложенного выполнения здесь не требуется.
    /// </summary>
    public void TeleportObject()
    {
        if (objectLists == null)
            return;

        // Объект, положенный на платформу, освобождаем и оставляем в мире.
        OrganTransfer.ReleaseAndDetach(platformSocket);
        if (platformSocket != null)
            platformSocket.socketActive = true;

        List<XRSocketInteractor> mainSockets = objectLists.mainSockets;
        List<XRSocketInteractor> teleportSockets = objectLists.teleportSockets;
        List<XRGrabInteractable> organs = objectLists.organs;

        // Все сокеты должны быть активны: иначе XRI освободит organ уже на следующем кадре.
        if (mainSockets != null)
        {
            for (int i = 0; i < mainSockets.Count; i++)
            {
                if (mainSockets[i] == null)
                    continue;

                mainSockets[i].socketActive = true;
                // В режиме конструктора любой орган можно положить в любой сокет.
                mainSockets[i].interactionLayers = everythingMask;
            }
        }

        if (teleportSockets != null)
        {
            for (int i = 0; i < teleportSockets.Count; i++)
            {
                if (teleportSockets[i] != null)
                    teleportSockets[i].socketActive = true;
            }
        }

        if (mainSockets == null || teleportSockets == null || organs == null)
            return;

        // Освобождаем сокеты заранее: освобождение и новый захват должны пройти
        // в одном кадре, иначе орган на мгновение станет динамическим и упадёт.
        OrganTransfer.ReleaseAll(mainSockets);
        OrganTransfer.ReleaseAll(teleportSockets);

        int count = Mathf.Min(teleportSockets.Count, organs.Count);
        for (int i = 0; i < count; i++)
        {
            if (teleportSockets[i] == null || organs[i] == null)
                continue;

            OrganTransfer.MoveToSocket(organs[i], teleportSockets[i]);
        }
    }
}

//  ___
// [o_0]
// /|_|\
//  | |
