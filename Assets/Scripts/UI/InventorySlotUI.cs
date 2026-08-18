using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One clickable inventory slot: an icon, a quantity label, and an optional
/// "selected" highlight (shown on the active hotbar slot). Instantiated by
/// InventoryUI - build a prefab with an Image, a Button, and (optionally) a
/// TextMeshProUGUI quantity label and a highlight GameObject, then assign the
/// matching references here.
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI quantityText;
    public GameObject selectedHighlight;

    [HideInInspector] public bool isHotbarSlot;
    [HideInInspector] public int slotIndex;

    public void Init(InventoryUI owner, bool isHotbarSlot, int slotIndex)
    {
        this.isHotbarSlot = isHotbarSlot;
        this.slotIndex = slotIndex;

        Button button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => owner.OnSlotClicked(this));

        SetEmpty();
    }

    public void SetEmpty()
    {
        if (iconImage != null) { iconImage.sprite = null; iconImage.enabled = false; }
        if (quantityText != null) quantityText.text = "";
        if (selectedHighlight != null) selectedHighlight.SetActive(false);
    }

    public void SetItem(Sprite icon, int quantity)
    {
        if (iconImage != null) { iconImage.sprite = icon; iconImage.enabled = icon != null; }
        if (quantityText != null) quantityText.text = quantity > 1 ? quantity.ToString() : "";
    }
}
