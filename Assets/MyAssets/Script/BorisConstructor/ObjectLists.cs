using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class ObjectLists : MonoBehaviour
{
    public List<XRSocketInteractor> mainSockets; // Ссылка на XRSocketInteractor
    public List<XRSocketInteractor> teleportSockets;
    public List<XRGrabInteractable> organs;

    /// <summary>
    /// Маски органов (не сокетов!). У органа в маске есть ОБЩИЙ слой, по которому
    /// контроллер берёт любой орган в руку. У сокета этого слоя быть не должно:
    /// если он есть у всех сокетов сразу, каждый сокет начинает принимать любой орган.
    /// </summary>
    public List<InteractionLayerMask> layerMasks;

    // Изначальные (сценовские) маски сокетов и органов.
    // Нужны потому, что маска органа и маска его сокета - РАЗНЫЕ: у костей в маске
    // органа есть общий для всех костей слой (бит 10), а у сокета - только его
    // собственный бит. Если при выходе из конструктора записать сокету маску органа
    // (как делал EndConstructor через layerMasks), общий слой появляется у всех
    // сокетов, и любую кость снова можно положить в любой сокет.
    private readonly List<InteractionLayerMask> m_InitialSocketMasks = new List<InteractionLayerMask>();
    private readonly List<InteractionLayerMask> m_InitialOrganMasks = new List<InteractionLayerMask>();

    private void Awake()
    {
        CaptureInitialMasks();
    }

    /// <summary>
    /// Запоминает маски, которые сокеты и органы получили в сцене.
    /// Вызывается в Awake - до того, как ButtonTeleporter/BeginAgain выставят
    /// сокетам общую маску режима конструктора.
    /// </summary>
    public void CaptureInitialMasks()
    {
        m_InitialSocketMasks.Clear();
        m_InitialOrganMasks.Clear();

        if (mainSockets != null)
        {
            for (int i = 0; i < mainSockets.Count; i++)
            {
                m_InitialSocketMasks.Add(mainSockets[i] != null
                    ? mainSockets[i].interactionLayers
                    : default(InteractionLayerMask));
            }
        }

        if (organs != null)
        {
            for (int i = 0; i < organs.Count; i++)
            {
                m_InitialOrganMasks.Add(organs[i] != null
                    ? organs[i].interactionLayers
                    : default(InteractionLayerMask));
            }
        }
    }

    /// <summary>
    /// Изначальная (сценовская) маска сокета с индексом index.
    /// </summary>
    /// <returns>false, если маска для этого индекса не запомнена.</returns>
    public bool TryGetInitialSocketMask(int index, out InteractionLayerMask mask)
    {
        if (index >= 0 && index < m_InitialSocketMasks.Count)
        {
            mask = m_InitialSocketMasks[index];
            return true;
        }

        mask = default(InteractionLayerMask);
        return false;
    }

    /// <summary>
    /// Изначальная (сценовская) маска органа с индексом index.
    /// </summary>
    /// <returns>false, если маска для этого индекса не запомнена.</returns>
    public bool TryGetInitialOrganMask(int index, out InteractionLayerMask mask)
    {
        if (index >= 0 && index < m_InitialOrganMasks.Count)
        {
            mask = m_InitialOrganMasks[index];
            return true;
        }

        mask = default(InteractionLayerMask);
        return false;
    }
}
