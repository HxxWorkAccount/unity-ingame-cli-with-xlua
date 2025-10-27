namespace UnityLearning {

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class Dragable
    : MonoBehaviour,
      IPointerDownHandler,
      IDragHandler
{
    [SerializeField]
    private RectTransform m_target;
    private bool          keepWithinParent       = false;  // TODO 这个实现好像有点问题，有空再看看，修好了转为 public
    public bool           dragWhenCursorInScreen = true;   // 防止将窗口拖出屏幕外

    private Vector2       m_pointerOffset;
    private RectTransform m_targetRect;
    private RectTransform m_parentRect;

    void Awake() {
        if (m_target == null)
            m_target = GetComponent<RectTransform>();
        m_targetRect = m_target;
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (m_targetRect == null)
            return;

        m_parentRect = m_targetRect.parent as RectTransform;

        // Calculate the offset of the pointer from the center of the object
        RectTransformUtility.ScreenPointToLocalPointInRectangle(  //
            m_targetRect, eventData.position, eventData.pressEventCamera, out m_pointerOffset
        );
    }

    public void OnDrag(PointerEventData eventData) {
        if (m_targetRect == null || m_parentRect == null)
            return;

        // Prevent dragging when cursor is off-screen
        if (dragWhenCursorInScreen) {
            if (eventData.position.x < 0 || eventData.position.x > Screen.width ||  //
                eventData.position.y < 0 || eventData.position.y > Screen.height) {
                return;
            }
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition
            )) {
            Vector3 targetPosition = localPointerPosition - m_pointerOffset;
            if (keepWithinParent)
                targetPosition = ClampToParent(targetPosition);
            m_targetRect.localPosition = targetPosition;
        }
    }

    private Vector2 ClampToParent(Vector2 localPosition) {
        Vector2 parentSize = m_parentRect.rect.size;
        Vector2 targetSize = m_targetRect.rect.size;

        Vector2 minPosition = (parentSize + targetSize) * -0.5f;
        Vector2 maxPosition = (parentSize + targetSize) * 0.5f;

        // Adjust for pivot
        Vector2 pivotOffset = new Vector2((0.5f - m_targetRect.pivot.x) * targetSize.x, (0.5f - m_targetRect.pivot.y) * targetSize.y);
        minPosition += pivotOffset;
        maxPosition += pivotOffset;

        return new Vector2(
            Mathf.Clamp(localPosition.x, minPosition.x, maxPosition.x), Mathf.Clamp(localPosition.y, minPosition.y, maxPosition.y)
        );
    }
}

}
