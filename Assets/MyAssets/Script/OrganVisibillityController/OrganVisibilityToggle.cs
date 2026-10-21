using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Переключает режим прозрачности материалов (Transparent + Additive) по нажатию кнопки.
/// При первом нажатии из Interaction Layer Mask контроллеров удаляются маски
/// из ObjectLists.layerMasks (контроллеры перестают взаимодействовать с объектами
/// на этих слоях), при повторном нажатии маски возвращаются.
/// </summary>
public class OrganVisibilityToggle : MonoBehaviour
{
    [Header("Ссылки")]
    public List<Material> materials;                 // Материалы, у которых меняется Surface Type
    public Button button;                            // Кнопка переключения
    public TextMeshProUGUI buttonText;                   // Текст на кнопке
    public ObjectLists objectLists;                  // Источник масок слоев (layerMasks)
    public XRBaseInteractor leftController;          // Левый контроллер
    public XRBaseInteractor rightController;         // Правый контроллер

    [Header("Кнопки состояний")]
    public Button[] showButtons;                     // Нажатие показывает органы (Opaque)
    public Button[] hideButtons;                     // Нажатие скрывает органы (Transparent + Additive)

    private bool isTransparent = false;              // Текущее состояние
    private int originalLeftLayers;                  // Исходная маска левого контроллера
    private int originalRightLayers;                 // Исходная маска правого контроллера

    private void OnEnable()
    {
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
    /// Показать органы: Opaque, исходные маски контроллеров, текст "Скрыть".
    /// </summary>
    public void Show()
    {
        ShowOpaque();
    }

    /// <summary>
    /// Скрыть органы: Transparent + Additive, слои удалены из масок контроллеров, текст "Показать".
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

    // Первое нажатие: Transparent + Additive, маска слоев, текст "Показать"
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

        RemoveLayersFromControllers();
    }

    // Повторное нажатие: Opaque, исходные маски слоев, текст "Скрыть"
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

        RestoreControllerLayers();
    }

    // Удалить из масок контроллеров все слои, перечисленные в ObjectLists.layerMasks
    private void RemoveLayersFromControllers()
    {
        if (objectLists == null || objectLists.layerMasks == null) return;

        // Собираем все удаляемые слои в одну маску
        int removedLayers = 0;
        foreach (InteractionLayerMask mask in objectLists.layerMasks)
            removedLayers |= (int)mask;

        if (leftController != null)
        {
            originalLeftLayers = leftController.interactionLayers;
            leftController.interactionLayers = originalLeftLayers & ~removedLayers;
        }

        if (rightController != null)
        {
            originalRightLayers = rightController.interactionLayers;
            rightController.interactionLayers = originalRightLayers & ~removedLayers;
        }
    }

    // Вернуть контроллерам их исходные маски слоев
    private void RestoreControllerLayers()
    {
        if (leftController != null)
            leftController.interactionLayers = originalLeftLayers;

        if (rightController != null)
            rightController.interactionLayers = originalRightLayers;
    }
}
