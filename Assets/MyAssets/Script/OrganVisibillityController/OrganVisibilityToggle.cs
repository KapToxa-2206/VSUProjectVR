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

    private bool isTransparent = false;              // Текущее состояние
    private int originalLeftLayers;                  // Исходная маска левого контроллера
    private int originalRightLayers;                 // Исходная маска правого контроллера

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(Toggle);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Toggle);
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
