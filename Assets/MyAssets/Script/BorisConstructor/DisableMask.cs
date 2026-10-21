using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class DisableMask : MonoBehaviour
{
    public InteractionLayerMask nothingMask; // Маска слоев для XR Grab Interactables
    public ObjectLists objectLists;
    public ColorLists colorLists;
    public XRBaseInteractor leftController;
    public XRBaseInteractor rightController;

    private void Start()
    {
        List<XRSocketInteractor> mainSockets = objectLists.mainSockets;
        for(int i = 0; i < mainSockets.Count; i++) {
            mainSockets[i].selectEntered.AddListener(OnSelectEntered);
            mainSockets[i].selectExited.AddListener(OnSelectExited);
            objectLists.organs[i].selectEntered.AddListener(OnSelectEnteredGrab);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        XRGrabInteractable grabInteractable = args.interactable.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.interactionLayers = nothingMask; // Отключить маску слоев для XR Grab Interactable
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        List<XRGrabInteractable> organs = objectLists.organs;
        XRGrabInteractable grabInteractable = args.interactable.GetComponent<XRGrabInteractable>();
        for(int i = 0; i < organs.Count; i++) {
            if (organs[i] != null && organs[i] == grabInteractable)
            {
                grabInteractable.interactionLayers = objectLists.layerMasks[i]; // Включить маску слоев для XR Grab Interactable
            }
        }
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