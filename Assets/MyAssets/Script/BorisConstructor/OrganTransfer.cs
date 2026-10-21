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
///  2) перенос   - объект ставится в позу attach-точки целевого сокета;
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

        // Переносим объект в позу attach-точки сокета. Именно её XRI использует
        // при фиксации объекта в сокете, поэтому результат совпадает с тем,
        // как объект выглядит в сокете при запуске игры.
        Transform attach = targetSocket.attachTransform != null ? targetSocket.attachTransform : targetSocket.transform;
        interactable.transform.SetPositionAndRotation(attach.position, attach.rotation);

        // Отдаём объект сокету в этом же кадре.
        Grab(targetSocket, interactable);

        return interactable.isSelected;
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
