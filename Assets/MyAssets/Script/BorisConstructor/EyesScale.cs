using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;

public class EyesScale : MonoBehaviour
{
    public ObjectLists objectLists;
    private List<Vector3> originalScale;
    public List<float> scaleMultiplier;

    private void Start()
    {
        for(int i = 0; i < objectLists.teleportSockets.Count; i++) {
            objectLists.teleportSockets[i].selectEntered.AddListener(OnSelectEnter);
            objectLists.teleportSockets[i].selectExited.AddListener(OnSelectExit);
        }
        originalScale = new List<Vector3>();
        for(int i = 0; i < objectLists.organs.Count; i++) {
            originalScale.Add(objectLists.organs[i].transform.localScale);
        }
    }

    private void OnSelectEnter(SelectEnterEventArgs args)
    {
        if(scaleMultiplier.Count != 0) {
            XRGrabInteractable interactable = args.interactable.GetComponent<XRGrabInteractable>();
            XRSocketInteractor socketInteractor = args.interactor.GetComponent<XRSocketInteractor>();
            for(int i = 0; i < objectLists.organs.Count; i++) {
                if(interactable.gameObject == objectLists.organs[i].gameObject) {
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
        for(int i = 0; i < objectLists.organs.Count; i++) {
            if(interactable.gameObject == objectLists.organs[i].gameObject) {
                interactable.transform.localScale = originalScale[i];
                socketInteractor.fixedScale =  new Vector3(1, 1, 1);
                break;
            }
        }
    }
}