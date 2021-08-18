using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CardRightClickHandler : MonoBehaviour, IPointerClickHandler
{
    public delegate void OnRightClick(Sprite image);
    public OnRightClick onRightClick;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && onRightClick != null)
        {
            onRightClick(gameObject.transform.GetChild(0).GetComponent<Image>().sprite);
        }
    }
}
