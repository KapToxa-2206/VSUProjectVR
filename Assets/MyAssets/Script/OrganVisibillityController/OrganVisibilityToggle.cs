using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

/// <summary>
/// Переключает режим прозрачности материалов (Transparent + Additive) по нажатию кнопки.
///
/// Пока орган (кость) прозрачный, контроллеры не могут его подсветить (Hover) или взять (Select).
/// Раньше для этого из Interaction Layer Mask контроллеров удалялись слои из
/// ObjectLists.layerMasks, а при возврате восстанавливались. Из-за этого ломался конструктор:
/// его кнопка старта тоже вызывает Hide(), поэтому маски контроллеров "накапливали"
/// удалённые слои (в т.ч. от ранее включённой прозрачности) и в итоге становились пустыми -
/// контроллеры не могли взять ни один орган или кость.
///
/// Теперь маски слоёв не меняются вообще. Вместо этого на контроллеры вешаются
/// hover/select-фильтры (штатный механизм XRI), которые запрещают взаимодействие
/// именно с прозрачными объектами. Сокеты при этом продолжают работать как раньше:
/// прозрачный объект не выпадает из сокета, потому что его собственная маска не меняется.
/// </summary>
public class OrganVisibilityToggle : MonoBehaviour
{
    [Header("Ссылки")]
    public List<Material> materials;                 // Материалы, у которых меняется Surface Type
    public Button button;                            // Кнопка переключения
    public TextMeshProUGUI buttonText;                   // Текст на кнопке
    public ObjectLists objectLists;                  // Органы/кости, которые становятся прозрачными
    public XRBaseInteractor leftController;          // Левый контроллер
    public XRBaseInteractor rightController;         // Правый контроллер

    // Общий для всех экземпляров OrganVisibilityToggle список прозрачных объектов:
    // фильтры контроллеров должны учитывать и органы, и кости.
    private static readonly HashSet<IXRInteractable> s_TransparentInteractables = new HashSet<IXRInteractable>();

    private static readonly XRHoverFilterDelegate s_HoverFilter =
        new XRHoverFilterDelegate(CanHoverTransparent);

    private static readonly XRSelectFilterDelegate s_SelectFilter =
        new XRSelectFilterDelegate(CanSelectTransparent);

    /// <summary>
    /// Статические данные не должны переживать перезапуск Play Mode при отключённом
    /// Domain Reload: иначе в списке останутся ссылки на объекты из прошлой сцены.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_TransparentInteractables.Clear();
    }

    [Header("Кнопки состояний")]
    public Button[] showButtons;                     // Нажатие показывает органы (Opaque)
    public Button[] hideButtons;                     // Нажатие скрывает органы (Transparent + Additive)

    private bool isTransparent = false;              // Текущее состояние

    private void OnEnable()
    {
        AddControllerFilters();

        if (button != null)
            button.onClick.AddListener(Toggle);

        if (showButtons != null)
            foreach (Button showButton in showButtons)
                if (showButton != null)
                    showButton.onClick.AddListener(Show);

        if (hideButtons != null)
            foreach (Button hideButton in hideButtons)
                if (hideButton != null)
                    hideButton.onClick.AddListener(Hide);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Toggle);

        if (showButtons != null)
            foreach (Button showButton in showButtons)
                if (showButton != null)
                    showButton.onClick.RemoveListener(Show);

        if (hideButtons != null)
            foreach (Button hideButton in hideButtons)
                if (hideButton != null)
                    hideButton.onClick.RemoveListener(Hide);
    }

    /// <summary>
    /// Показать органы: Opaque, текст "Скрыть". Interaction Layer Mask контроллеров не меняется.
    /// </summary>
    public void Show()
    {
        ShowOpaque();
    }

    /// <summary>
    /// Скрыть органы: Transparent + Additive, текст "Показать". Interaction Layer Mask контроллеров не меняется.
    /// </summary>
    public void Hide()
    {
        ShowTransparent();
    }

    /// <summary>
    /// Переключение между прозрачным (Transparent + Additive) и непрозрачным (Opaque) режимами.
    /// </summary>
    public void Toggle()
    {
        if (isTransparent)
            ShowOpaque();
        else
            ShowTransparent();
    }

    // Первое нажатие: Transparent + Additive, текст "Показать"
    private void ShowTransparent()
    {
        isTransparent = true;

        foreach (Material mat in materials)
        {
            if (mat == null) continue;

            mat.SetFloat("_Surface", 1f);                              // Surface Type: Transparent
            mat.SetFloat("_Blend", 2f);                                // Blending Mode: Additive
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        if (buttonText != null)
            buttonText.text = "Показать";

        // Пока объекты прозрачные, контроллеры не должны с ними взаимодействовать.
        SetObjectsTransparent(true);
    }

    // Повторное нажатие: Opaque, текст "Скрыть"
    private void ShowOpaque()
    {
        isTransparent = false;

        foreach (Material mat in materials)
        {
            if (mat == null) continue;

            mat.SetFloat("_Surface", 0f);                              // Surface Type: Opaque
            mat.SetFloat("_Blend", 0f);                                // Blending Mode: Alpha
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.SetInt("_SrcBlend", (int)BlendMode.One);
            mat.SetInt("_DstBlend", (int)BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Geometry;
        }

        if (buttonText != null)
            buttonText.text = "Скрыть";

        // Объекты снова непрозрачные - взаимодействие разрешено.
        SetObjectsTransparent(false);
    }

    /// <summary>
    /// Добавляет на контроллеры hover/select-фильтры, запрещающие взаимодействие
    /// с прозрачными органами и костями. Фильтры общие (static), а список фильтров
    /// XRI сам не добавляет один и тот же экземпляр дважды.
    /// </summary>
    private void AddControllerFilters()
    {
        AddFilters(leftController);
        AddFilters(rightController);
    }

    private static void AddFilters(XRBaseInteractor controller)
    {
        if (controller == null)
            return;

        controller.hoverFilters.Add(s_HoverFilter);
        controller.selectFilters.Add(s_SelectFilter);
    }

    /// <summary>
    /// Помечает органы (кости) этого экземпляра как прозрачные или снимает пометку.
    /// </summary>
    private void SetObjectsTransparent(bool transparent)
    {
        if (objectLists == null || objectLists.organs == null)
            return;

        for (int i = 0; i < objectLists.organs.Count; i++)
        {
            XRGrabInteractable organ = objectLists.organs[i];
            if (organ == null)
                continue;

            if (transparent)
                s_TransparentInteractables.Add(organ);
            else
                s_TransparentInteractables.Remove(organ);
        }
    }

    private static bool CanHoverTransparent(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
    {
        return CanInteractWith(interactable);
    }

    private static bool CanSelectTransparent(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        return CanInteractWith(interactable);
    }

    private static bool CanInteractWith(IXRInteractable interactable)
    {
        if (interactable == null)
            return true;

        // Уничтоженный объект в static-списке не должен блокировать взаимодействие.
        if (interactable is Object unityObject && unityObject == null)
            return true;

        return !s_TransparentInteractables.Contains(interactable);
    }
}
