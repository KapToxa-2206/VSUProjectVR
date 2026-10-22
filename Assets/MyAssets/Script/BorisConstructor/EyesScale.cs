using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;

/// <summary>
/// Масштабирует объекты, находящиеся в телепорт-сокетах, и возвращает им
/// исходный размер, когда они извлекаются из сокета.
///
/// Важный нюанс: хранить нужно МИРОВОЙ масштаб (lossyScale), а не локальный.
/// XRI при захвате объекта отцепляет его от родителя
/// (XRGrabInteractable.Grab -> transform.SetParent(null)), при этом Unity
/// пересчитывает локальный масштаб так, чтобы сохранить мировой.
/// У костей родителем является узел "Skeleton" с масштабом 0.000989,
/// поэтому локальный масштаб кости меняется с 1 на 0.000989.
/// Если затем вернуть старый локальный масштаб (1), кость "раздувается"
/// примерно в 1000 раз: камера оказывается внутри меша (модель не видна,
/// видна только изнанка, отсекаемая back-face culling'ом) и луч контроллера
/// не попадает в коллайдер, внутри которого он находится, - с костью
/// невозможно взаимодействовать.
/// </summary>
public class EyesScale : MonoBehaviour
{
    public ObjectLists objectLists;

    /// <summary>
    /// Исходный МИРОВОЙ масштаб (lossyScale) каждого объекта из objectLists.organs.
    /// </summary>
    private List<Vector3> originalScale;

    public List<float> scaleMultiplier;

    private void Start()
    {
        if (objectLists == null)
            return;

        for(int i = 0; i < objectLists.teleportSockets.Count; i++) {
            if (objectLists.teleportSockets[i] == null)
                continue;

            objectLists.teleportSockets[i].selectEntered.AddListener(OnSelectEnter);
            objectLists.teleportSockets[i].selectExited.AddListener(OnSelectExit);
        }

        originalScale = new List<Vector3>();
        for(int i = 0; i < objectLists.organs.Count; i++) {
            // Сохраняем мировой масштаб: локальный зависит от масштаба родителя
            // и меняется, когда XRI отцепляет объект от иерархии.
            originalScale.Add(objectLists.organs[i] != null
                ? objectLists.organs[i].transform.lossyScale
                : Vector3.one);
        }
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        if(scaleMultiplier.Count != 0) {
            XRGrabInteractable interactable = args.interactable.GetComponent<XRGrabInteractable>();
            XRSocketInteractor socketInteractor = args.interactor.GetComponent<XRSocketInteractor>();
            if (interactable == null || socketInteractor == null)
                return;

            for(int i = 0; i < objectLists.organs.Count; i++) {
                if(objectListContains(i) && interactable.gameObject == objectLists.organs[i].gameObject) {
                    socketInteractor.fixedScale =  new Vector3(1, 1, 1) * scaleMultiplier[i];
                    break;
                }
            }
        }
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        XRGrabInteractable interactable = args.interactable.GetComponent<XRGrabInteractable>();
        XRSocketInteractor socketInteractor = args.interactor.GetComponent<XRSocketInteractor>();
        if (interactable == null || socketInteractor == null)
            return;

        for(int i = 0; i < objectLists.organs.Count; i++) {
            if(objectListContains(i) && interactable.gameObject == objectLists.organs[i].gameObject) {
                // Возвращаем мировой масштаб: локальный считается от текущего
                // родителя, поэтому после отцепления от "Skeleton" он уже не равен 1.
                SetWorldScale(interactable.transform, originalScale[i]);
                socketInteractor.fixedScale =  new Vector3(1, 1, 1);
                break;
            }
        }
    }

    /// <summary>
    /// Проверяет, что в списке объектов есть элемент с указанным индексом
    /// и для него сохранён исходный масштаб.
    /// </summary>
    private bool objectListContains(int index)
    {
        if (objectLists == null || objectLists.organs == null)
            return false;

        if (index < 0 || index >= objectLists.organs.Count)
            return false;

        if (objectLists.organs[index] == null)
            return false;

        return originalScale != null && index < originalScale.Count;
    }

    /// <summary>
    /// Задаёт локальный масштаб объекта так, чтобы его МИРОВОЙ масштаб
    /// стал равен <paramref name="worldScale"/>.
    /// </summary>
    /// <param name="target">Объект, масштаб которого нужно восстановить.</param>
    /// <param name="worldScale">Требуемый мировой масштаб.</param>
    private static void SetWorldScale(Transform target, Vector3 worldScale)
    {
        if (target == null)
            return;

        Vector3 parentScale = target.parent != null ? target.parent.lossyScale : Vector3.one;

        target.localScale = new Vector3(
            parentScale.x != 0f ? worldScale.x / parentScale.x : worldScale.x,
            parentScale.y != 0f ? worldScale.y / parentScale.y : worldScale.y,
            parentScale.z != 0f ? worldScale.z / parentScale.z : worldScale.z);
    }
}
