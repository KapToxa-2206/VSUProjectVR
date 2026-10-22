using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Детерминированное перемещение органов между сокетами XR Interaction Toolkit.
///
/// Зачем нужен этот класс:
///  У сокета нет мгновенного "отпускания". Когда у него выставляют socketActive = false,
///  XR Interaction Toolkit освобождает объект только позже - в обновлении XRInteractionManager.
///  Если в этот момент вручную записать transform.position/rotation, запись затрётся
///  всё ещё активным захватом, и орган останется на старом месте (отсюда "работает не всегда").
///
/// Как устроено перемещение здесь:
///  1) Release  - синхронное освобождение объекта через XRInteractionManager.SelectExit;
///  2) перенос   - объект ставится ровно в ту позу, в которую его "защёлкнул" бы сокет;
///  3) Grab     - синхронная передача объекта сокету через XRInteractionManager.SelectEnter.
///
/// Освобождение и новый захват происходят в одном кадре, поэтому XRGrabInteractable
/// не успевает выполнить Detach() (он вызывается только когда объект больше не выбран):
/// орган не падает под гравитацией и не получает импульс броска.
/// Кроме того, новый сокет сразу забирает объект себе, поэтому старый сокет
/// уже не может перехватить его обратно (XRSocketInteractor.CanSelect возвращает false,
/// пока объект выбран кем-то ещё).
/// </summary>
public static class OrganTransfer
{
    /// <summary>
    /// Перемещает объект в сокет: освобождает его у текущего держателя и отдаёт целевому сокету.
    /// </summary>
    /// <param name="interactable">Перемещаемый объект (орган).</param>
    /// <param name="targetSocket">Сокет, в который перемещается объект.</param>
    /// <returns>true, если объект после перемещения выбран целевым сокетом.</returns>
    public static bool MoveToSocket(XRBaseInteractable interactable, XRSocketInteractor targetSocket)
    {
        if (interactable == null)
            return false;

        if (targetSocket == null)
        {
            ReleaseFromAll(interactable);
            return false;
        }

        // Сокет должен быть активен, иначе XRI освободит объект уже на следующем кадре.
        targetSocket.socketActive = true;

        // Сокет удерживает только один объект - освобождаем прежнего.
        Release(targetSocket);

        // Забираем объект у того, кто его держит сейчас (старый сокет, контроллер).
        // Освобождение должно произойти до переноса, иначе Grab() запомнит
        // неправильного родителя и вернёт объект не туда.
        ReleaseFromAll(interactable);

        // Отцепляем объект от родителя, сохраняя мировое положение, поворот и масштаб.
        // XRI делает то же самое при захвате (XRGrabInteractable.Grab -> SetParent(null)),
        // но уже после того, как объект получил новую позу. Делаем это заранее, чтобы
        // локальный масштаб объекта не зависел от масштаба родителя: у костей родитель -
        // узел "Skeleton" с масштабом 0.000989, поэтому после отцепления локальный
        // масштаб кости меняется с 1 на 0.000989. Если объект оставить вложенным,
        // любой код, работающий с локальным масштабом, "раздует" кость в тысячу раз.
        DetachPreservingWorldTransform(interactable.transform);

        // Переносим объект в позу, в которую его "защёлкивает" сокет.
        // Именно её XRI использует при фиксации объекта в сокете, поэтому результат
        // совпадает с тем, как объект выглядит в сокете при запуске игры.
        GetSocketSnapPose(targetSocket, interactable, out Vector3 position, out Quaternion rotation);
        interactable.transform.SetPositionAndRotation(position, rotation);

        // Отдаём объект сокету в этом же кадре.
        Grab(targetSocket, interactable);

        return interactable.isSelected;
    }

    /// <summary>
    /// Вычисляет мировую позу, в которую сокет XR Interaction Toolkit "защёлкивает" объект.
    ///
    /// Формула полностью повторяет XRSocketGrabTransformer.GetTargetPoseForInteractable:
    ///   positionOffset  = attachTransform.InverseTransformDirection(objPos - attachPos)
    ///   targetPos       = socketRot * positionOffset + socketPos
    ///   rotationOffset  = Inverse(Inverse(objRot) * attachRot)
    ///   targetRot       = socketRot * rotationOffset
    ///
    /// Зачем это нужно:
    ///  XRI размещает в сокете не сам объект, а его attach-точку (m_AttachTransform).
    ///  У костей скелета attach-точка сильно смещена относительно начала координат модели
    ///  (у черепа - на 1.7 м), поэтому "начало координат объекта" в сокет встать не может.
    ///  Кроме того, XRI "защёлкивает" объект только если тот находится в радиусе
    ///  m_SocketSnappingRadius от целевой точки (у основных сокетов - 0.1 м).
    ///  Если поставить объект началом координат в точку сокета, проверка радиуса не
    ///  пройдёт, и объект останется в неверном месте (именно так и возвращались кости).
    ///  Если считать позу напрямую, результат не зависит от радиуса и всегда совпадает
    ///  с тем, что сделал бы сам XR Interaction Toolkit.
    /// </summary>
    /// <param name="socket">Сокет, в который защёлкивается объект.</param>
    /// <param name="interactable">Объект, который защёлкивается.</param>
    /// <param name="position">Мировая позиция объекта в сокете.</param>
    /// <param name="rotation">Мировой поворот объекта в сокете.</param>
    static void GetSocketSnapPose(XRSocketInteractor socket, XRBaseInteractable interactable, out Vector3 position, out Quaternion rotation)
    {
        // Attach-точка сокета (если не задана - сам сокет).
        Transform socketAttach = socket.attachTransform != null ? socket.attachTransform : socket.transform;

        Transform objectTransform = interactable.transform;

        // Attach-точка объекта. Важно: считать её нужно, пока объект ещё никем
        // не выбран - тогда XRI возвращает статическую m_AttachTransform, а не
        // динамическую (она создаётся только при захвате).
        Transform objectAttach = interactable.GetAttachTransform(socket);
        if (objectAttach == null)
            objectAttach = objectTransform;

        // Смещение начала координат объекта относительно его attach-точки,
        // переведённое в локальные координаты attach-точки.
        Vector3 attachOffset = objectTransform.position - objectAttach.position;
        Vector3 positionOffset = objectAttach.InverseTransformDirection(attachOffset);

        // Поворот сохраняет взаимное положение объекта и его attach-точки.
        Quaternion rotationOffset = Quaternion.Inverse(Quaternion.Inverse(objectTransform.rotation) * objectAttach.rotation);

        position = socketAttach.rotation * positionOffset + socketAttach.position;
        rotation = socketAttach.rotation * rotationOffset;
    }

    /// <summary>
    /// Освобождает объект, который удерживает сокет (если сокет что-то удерживает).
    /// </summary>
    public static void Release(XRSocketInteractor socket)
    {
        if (socket == null)
            return;

        IXRSelectInteractable selected = socket.GetOldestInteractableSelected();
        if (selected != null)
            Release(socket, selected);
    }

    /// <summary>
    /// Освобождает объект из сокета и отвязывает его от сокета (объект остаётся в мире).
    /// </summary>
    public static void ReleaseAndDetach(XRSocketInteractor socket)
    {
        if (socket == null)
            return;

        IXRSelectInteractable selected = socket.GetOldestInteractableSelected();
        if (selected == null)
            return;

        Release(socket, selected);

        if (selected is XRBaseInteractable interactable)
            interactable.transform.SetParent(null);
    }

    /// <summary>
    /// Освобождает все сокеты из списка (каждый - от объекта, который он удерживает).
    /// </summary>
    public static void ReleaseAll(List<XRSocketInteractor> sockets)
    {
        if (sockets == null)
            return;

        for (int i = 0; i < sockets.Count; i++)
            Release(sockets[i]);
    }

    /// <summary>
    /// Освобождает объект от всех, кто его держит (сокеты, контроллеры).
    /// </summary>
    public static void ReleaseFromAll(XRBaseInteractable interactable)
    {
        if (interactable == null)
            return;

        List<IXRSelectInteractor> selectors = interactable.interactorsSelecting;
        if (selectors == null || selectors.Count == 0)
            return;

        // Копия: во время выхода из выбора сам список меняется.
        var copy = new List<IXRSelectInteractor>(selectors);
        for (int i = 0; i < copy.Count; i++)
        {
            if (copy[i] == null)
                continue;

            Release(copy[i], interactable);
        }
    }

    /// <summary>
    /// Отцепляет объект от родителя, сохраняя мировое положение, поворот и масштаб.
    ///
    /// Зачем это нужно:
    ///  Локальный масштаб объекта зависит от масштаба родителя. При отцеплении
    ///  Unity пересчитывает локальный масштаб так, чтобы мировой остался прежним
    ///  (у костей родитель - узел "Skeleton" с масштабом 0.000989, поэтому локальный
    ///  масштаб кости меняется с 1 на 0.000989). Если после этого кто-то запишет
    ///  старый локальный масштаб (1), объект станет примерно в тысячу раз больше:
    ///  камера окажется внутри меша (модель не видна) и луч контроллера не будет
    ///  попадать в коллайдер, внутри которого он находится (нет взаимодействия).
    /// </summary>
    /// <param name="target">Объект, который нужно отцепить от родителя.</param>
    static void DetachPreservingWorldTransform(Transform target)
    {
        if (target == null || target.parent == null)
            return;

        // worldPositionStays = true: мировое положение, поворот и масштаб сохраняются.
        target.SetParent(null, true);
    }

    /// <summary>
    /// Синхронно освобождает объект у интерактора.
    /// </summary>
    static void Release(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        XRInteractionManager manager = GetManager(interactor, interactable);
        if (manager == null)
            return;

        manager.SelectExit(interactor, interactable);
    }

    /// <summary>
    /// Синхронно передаёт объект интерактору.
    /// </summary>
    static void Grab(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        XRInteractionManager manager = GetManager(interactor, interactable);
        if (manager == null)
            return;

        manager.SelectEnter(interactor, interactable);
    }

    /// <summary>
    /// Возвращает менеджер взаимодействий, через который выполняются SelectExit/SelectEnter.
    /// </summary>
    static XRInteractionManager GetManager(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactor is XRBaseInteractor baseInteractor && baseInteractor.interactionManager != null)
            return baseInteractor.interactionManager;

        if (interactable is XRBaseInteractable baseInteractable && baseInteractable.interactionManager != null)
            return baseInteractable.interactionManager;

        return null;
    }
}
