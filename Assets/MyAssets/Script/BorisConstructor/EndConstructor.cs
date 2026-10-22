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
        if (placementButton != null)
            placementButton.onClick.AddListener(PlaceObject); // Добавляем слушатель для события "onClick" кнопки    
    }

    /// <summary>
    /// Выход из конструктора: органы возвращаются в основные сокеты,
    /// маски слоёв восстанавливаются, счётчик сбрасывается.
    /// Перемещение выполняется синхронно через API XR Interaction Toolkit,
    /// поэтому отложенного выполнения здесь не требуется.
    /// </summary>
    private void PlaceObject()
    {
        if (objectLists == null)
            return;

        // Объект, положенный на платформу, освобождаем и оставляем в мире.
        OrganTransfer.ReleaseAndDetach(platformSocket);
        if (platformSocket != null)
            platformSocket.socketActive = true;

        List<XRSocketInteractor> mainSockets = objectLists.mainSockets;
        List<XRGrabInteractable> organs = objectLists.organs;
        List<InteractionLayerMask> layerMasks = objectLists.layerMasks;

        if (mainSockets == null || organs == null)
            return;

        int count = Mathf.Min(mainSockets.Count, organs.Count);

        // Восстанавливаем маски слоёв: в каждом сокете должен помещаться только свой орган.
        // Сокету возвращаем ЕГО изначальную маску, а не layerMasks[i]: layerMasks - это
        // маски органов, и в них есть общий для всех органов слой (у костей - бит 10),
        // по которому контроллер берёт орган в руку. Если записать эту маску сокетам,
        // общий слой окажется у всех сокетов сразу, и тогда любую кость можно будет
        // положить в любой сокет (именно так и ломались кости).
        for (int i = 0; i < mainSockets.Count; i++)
        {
            if (mainSockets[i] == null)
                continue;

            mainSockets[i].socketActive = true;

            if (objectLists.TryGetInitialSocketMask(i, out InteractionLayerMask socketMask))
                mainSockets[i].interactionLayers = socketMask;
            else if (layerMasks != null && i < layerMasks.Count)
                mainSockets[i].interactionLayers = layerMasks[i];
        }

        if (colorLists != null)
            colorLists.RestoreAll(); // Возвращаем органам изначальный вид

        // Освобождаем сокеты заранее: освобождение и новый захват должны пройти
        // в одном кадре, иначе орган на мгновение станет динамическим и упадёт.
        OrganTransfer.ReleaseAll(mainSockets);

        for (int i = 0; i < count; i++)
        {
            if (mainSockets[i] == null || organs[i] == null)
                continue;

            // Маска органа должна совпадать с маской сокета, иначе XRI
            // освободит орган уже на следующем кадре.
            if (objectLists.TryGetInitialOrganMask(i, out InteractionLayerMask organMask))
                organs[i].interactionLayers = organMask;
            else if (layerMasks != null && i < layerMasks.Count)
                organs[i].interactionLayers = layerMasks[i];

            OrganTransfer.MoveToSocket(organs[i], mainSockets[i]);
        }

        if (textMeshPro != null)
            textMeshPro.text = "0 из " + mainSockets.Count.ToString("D1");
    }
}
