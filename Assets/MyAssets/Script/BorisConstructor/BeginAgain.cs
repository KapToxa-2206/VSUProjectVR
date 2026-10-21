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
    public InteractionLayerMask everythingMask;
    // public XRController leftController;
    // public XRController rightController;

    private void Start()
    {
        if(placementButton != null) {
            placementButton.onClick.AddListener(TeleportObject); // Добавляем слушатель для события "onClick" кнопки 
        }  
    }

    /// <summary>
    /// Перезапуск игры: органы возвращаются в телепорт-сокеты,
    /// счётчик сбрасывается, органы получают изначальный вид.
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

        // Все сокеты должны быть активны: иначе XRI освободит орган уже на следующем кадре.
        if (mainSockets != null)
        {
            for (int i = 0; i < mainSockets.Count; i++)
            {
                if (mainSockets[i] == null)
                    continue;

                mainSockets[i].socketActive = true;
                // После перезапуска состояние должно совпадать с началом игры:
                // в режиме конструктора любой орган можно положить в любой сокет.
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

        if (colorLists != null)
            colorLists.RestoreAll(); // Возвращаем органам изначальный вид

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

        if (textMeshPro != null)
            textMeshPro.text = "0 из " + mainSockets.Count.ToString("D1");
    }
}
