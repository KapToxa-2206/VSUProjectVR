using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using System.Collections.Generic;

/// <summary>
/// Убирает "лишний" Hover у соседних сокетов в игре-конструкторе.
///
/// Как задумана игра:
///  - в режиме конструктора у всех сокетов становится ОБЩАЯ Interaction Layer Mask
///    (это делает ButtonTeleporter), поэтому любой свободный орган подсвечивается
///    (Hover) у любого сокета - так пользователь может проверить любое место;
///  - как только орган установлен в сокет, он НЕ должен подсвечиваться у соседних сокетов;
///  - при этом сокет должен продолжать удерживать установленный орган, а контроллеры -
///    по-прежнему должны иметь возможность взять этот орган в руку.
///
/// Раньше третий пункт делался так: при selectEntered сокета маска слоёв органа
/// заменялась на nothingMask (бит 8, его есть только у контроллеров).
/// Из-за этого ломалась сама фиксация органа в сокете: XRI каждый кадр проверяет
/// пересечение масок сокета и органа (XRInteractionManager.ClearInteractorSelection ->
/// CanSelect -> HasInteractionLayerOverlap). Пересечение исчезало, сокет снимал выбор
/// (XRGrabInteractable.Drop -> Rigidbody снова становится динамическим, включается гравитация),
/// и орган выталкивался из сокета физикой (PhysX). Плюс общий бит nothingMask не позволял
/// отличить один установленный орган от другого - при двух и более установленных органах
/// они начинали подсвечивать сокеты друг друга.
///
/// Теперь маски слоёв не меняются вообще, а запрет Hover сделан штатным механизмом XRI -
/// hover-фильтром сокета (IXRHoverFilter). Фильтр разрешает Hover органа только если этот
/// орган не удерживается другим сокетом из списка. Благодаря этому:
///  - свободный орган по-прежнему подсвечивается у любого сокета (маска общая);
///  - установленный орган не подсвечивается у соседних сокетов;
///  - сокет не теряет выбор, орган остаётся кинематическим и не выталкивается;
///  - контроллеры могут взять установленный орган (бит органа есть в маске контроллеров).
/// </summary>
public class DisableMask : MonoBehaviour
{
    [Tooltip("Не используется. Раньше маской слоёв органа заменялась его собственная маска. Оставлено, чтобы не ломать сцену.")]
    public InteractionLayerMask nothingMask;

    public ObjectLists objectLists;
    public ColorLists colorLists;
    public XRBaseInteractor leftController;
    public XRBaseInteractor rightController;

    // Сокеты, между которыми запрещён Hover уже установленных органов
    private readonly List<XRSocketInteractor> m_Sockets = new List<XRSocketInteractor>();

    private void Awake()
    {
        if (objectLists == null || objectLists.mainSockets == null)
            return;

        for (int i = 0; i < objectLists.mainSockets.Count; i++)
        {
            XRSocketInteractor socket = objectLists.mainSockets[i];
            if (socket == null)
                continue;

            m_Sockets.Add(socket);
        }

        if (m_Sockets.Count == 0)
            return;

        // Один общий фильтр на все сокеты - вручную в инспекторе ничего настраивать не нужно
        IXRHoverFilter filter = new XRHoverFilterDelegate(CanHoverOrgan);
        for (int i = 0; i < m_Sockets.Count; i++)
        {
            m_Sockets[i].hoverFilters.Add(filter);
        }
    }

    private void Start()
    {
        if (objectLists == null || objectLists.organs == null)
            return;

        for (int i = 0; i < objectLists.organs.Count; i++)
        {
            XRGrabInteractable organ = objectLists.organs[i];
            if (organ == null)
                continue;

            organ.selectEntered.AddListener(OnSelectEnteredGrab);
        }
    }

    /// <summary>
    /// Hover-фильтр сокета: установленный в другом сокете орган подсвечивать нельзя.
    /// </summary>
    /// <param name="interactor">Сокет, который спрашивает разрешение на Hover.</param>
    /// <param name="interactable">Орган, который сокет хочет подсветить.</param>
    /// <returns>false, если орган уже удерживается другим сокетом.</returns>
    private bool CanHoverOrgan(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        if (!(interactable is IXRSelectInteractable selectInteractable))
            return true;

        List<IXRSelectInteractor> selecting = selectInteractable.interactorsSelecting;
        for (int i = 0; i < selecting.Count; i++)
        {
            // Если орган удерживается сокетом (не этим) - соседние сокеты его не подсвечивают
            if (selecting[i] is XRSocketInteractor socket
                && !ReferenceEquals(socket, interactor)
                && m_Sockets.Contains(socket))
            {
                return false;
            }
        }

        return true;
    }

    private void OnSelectEnteredGrab(SelectEnterEventArgs args)
    {
        XRGrabInteractable grabInteractable = args.interactable.GetComponent<XRGrabInteractable>();
        XRBaseInteractor controller = args.interactor.GetComponent<XRBaseInteractor>();
        if(controller == leftController || controller == rightController) {
            for(int i = 0; i < objectLists.organs.Count; i++) {
                if (grabInteractable == objectLists.organs[i])
                {
                    colorLists.RestoreOrgan(i); // При перемещении органа возвращаем ему изначальный вид
                }
            }
        }
    }
}
