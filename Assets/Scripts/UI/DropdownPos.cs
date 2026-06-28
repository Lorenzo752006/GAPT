using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownPos : MonoBehaviour
{
    [Header("Dropdown List Position")]
    [SerializeField] private float listHeight = 200f;
    [SerializeField] private float verticalOffset = -2f;

    private TMP_Dropdown dropdown;
    private RectTransform dropdownRect;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdownRect = GetComponent<RectTransform>();

        FixTemplatePosition();
    }

    private void OnEnable()
    {
        FixTemplatePosition();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (dropdown == null)
        {
            return;
        }

        FixTemplatePosition();
    }

    private void LateUpdate()
    {
        FixTemplatePosition();
        FixRuntimeDropdownListPosition();
    }

    private void FixTemplatePosition()
    {
        if (dropdown == null || dropdown.template == null)
        {
            return;
        }

        RectTransform templateRect = dropdown.template;

        if (templateRect.parent != transform)
        {
            templateRect.SetParent(transform, false);
        }

        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);

        templateRect.anchoredPosition = new Vector2(0f, verticalOffset);
        templateRect.sizeDelta = new Vector2(0f, listHeight);

        templateRect.localScale = Vector3.one;
        templateRect.localRotation = Quaternion.identity;
    }

    private void FixRuntimeDropdownListPosition()
    {
        Transform dropdownListTransform = transform.Find("Dropdown List");

        if (dropdownListTransform == null)
        {
            GameObject dropdownListObject = GameObject.Find("Dropdown List");

            if (dropdownListObject == null)
            {
                return;
            }

            dropdownListTransform = dropdownListObject.transform;
        }

        RectTransform dropdownListRect = dropdownListTransform as RectTransform;

        if (dropdownListRect == null)
        {
            return;
        }

        if (dropdownListRect.parent != transform)
        {
            dropdownListRect.SetParent(transform, false);
        }

        dropdownListRect.anchorMin = new Vector2(0f, 0f);
        dropdownListRect.anchorMax = new Vector2(1f, 0f);
        dropdownListRect.pivot = new Vector2(0.5f, 1f);

        dropdownListRect.anchoredPosition = new Vector2(0f, verticalOffset);
        dropdownListRect.sizeDelta = new Vector2(0f, listHeight);

        dropdownListRect.localScale = Vector3.one;
        dropdownListRect.localRotation = Quaternion.identity;
    }
}