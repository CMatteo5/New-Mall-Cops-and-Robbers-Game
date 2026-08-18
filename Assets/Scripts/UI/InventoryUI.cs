using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Drives the hotbar (always visible) and backpack/storage panel (toggle with Tab) for
/// the LOCAL player's PlayerInventory. Assign the containers and an InventorySlotUI
/// prefab in the Inspector; this instantiates one slot per inventory slot the first
/// time a local PlayerInventory is found (i.e. once the local player has spawned).
///
/// Click-to-move: click one slot, then click a second slot to swap the two items.
/// Number keys 1-9 select a hotbar slot; Tab toggles the storage panel.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Hotbar")]
    [SerializeField] private Transform hotbarContainer;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Storage / Backpack")]
    [SerializeField] private GameObject storagePanel;
    [SerializeField] private Transform storageContainer;

    private readonly List<InventorySlotUI> hotbarSlots = new List<InventorySlotUI>();
    private readonly List<InventorySlotUI> storageSlots = new List<InventorySlotUI>();

    private PlayerInventory inventory;
    private InventorySlotUI selectedSlot;

    private void Update()
    {
        // Wait for the local player's inventory to exist (it spawns with the player).
        if (inventory == null)
        {
            if (PlayerInventory.Local == null) return;
            Bind(PlayerInventory.Local);
        }

        HandleHotbarHotkeys();

        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            ToggleStorage();
    }

    private void Bind(PlayerInventory playerInventory)
    {
        inventory = playerInventory;
        BuildSlots(hotbarContainer, hotbarSlots, inventory.HotbarSize, isHotbar: true);
        BuildSlots(storageContainer, storageSlots, inventory.StorageSize, isHotbar: false);
        inventory.OnInventoryChanged += Refresh;
        if (storagePanel != null) storagePanel.SetActive(false);
        Refresh();
    }

    private void BuildSlots(Transform container, List<InventorySlotUI> list, int count, bool isHotbar)
    {
        if (container == null || slotPrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            InventorySlotUI slot = Instantiate(slotPrefab, container);
            slot.Init(this, isHotbar, i);
            list.Add(slot);
        }
    }

    private void Refresh()
    {
        if (inventory == null) return;
        RefreshList(hotbarSlots, isHotbar: true);
        RefreshList(storageSlots, isHotbar: false);
    }

    private void RefreshList(List<InventorySlotUI> slots, bool isHotbar)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotData data = isHotbar ? inventory.GetHotbarSlot(i) : inventory.GetStorageSlot(i);

            // Selection highlight is independent of whether the slot has an item in it -
            // an empty hotbar slot can still be the SELECTED slot (e.g. right after
            // dropping/using whatever was in it), so this must not be skipped for empty
            // slots the way SetEmpty()/SetItem() are.
            bool isSelected = isHotbar && i == inventory.SelectedHotbarIndex;

            if (data.IsEmpty)
            {
                slots[i].SetEmpty();
            }
            else
            {
                ItemData itemData = FindItemData(data.itemName.ToString());
                slots[i].SetItem(itemData != null ? itemData.icon : null, data.quantity);
            }

            if (slots[i].selectedHighlight != null)
                slots[i].selectedHighlight.SetActive(isSelected);
        }
    }

    private ItemData FindItemData(string itemNameToFind)
    {
        if (inventory.Registry == null || inventory.Registry.items == null) return null;

        foreach (ItemData item in inventory.Registry.items)
            if (item != null && item.itemName == itemNameToFind) return item;

        return null;
    }

    private void HandleHotbarHotkeys()
    {
        if (Keyboard.current == null) return;

        for (int i = 0; i < hotbarSlots.Count && i < 9; i++)
        {
            Key key = Key.Digit1 + i;
            if (Keyboard.current[key].wasPressedThisFrame)
                inventory.SelectHotbarSlot(i);
        }
    }

    private void ToggleStorage()
    {
        if (storagePanel != null) storagePanel.SetActive(!storagePanel.activeSelf);
    }

    /// <summary>Click-to-move: click a slot to pick it up, click a second slot to swap. Clicking the same slot again cancels.</summary>
    public void OnSlotClicked(InventorySlotUI clicked)
    {
        // Immediately clear the EventSystem's UI selection after any click on a slot.
        // Otherwise the clicked Button stays "selected", and since this project's
        // Input System UI navigation is bound to WASD (not just arrow keys), that
        // leftover selection silently steals WASD away from player movement even
        // after you click back into the Game view - it doesn't clear on its own.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (selectedSlot == null)
        {
            selectedSlot = clicked;
            return;
        }

        if (selectedSlot == clicked)
        {
            selectedSlot = null;
            return;
        }

        inventory.RequestMoveSlot(selectedSlot.isHotbarSlot, selectedSlot.slotIndex,
                                   clicked.isHotbarSlot, clicked.slotIndex);
        selectedSlot = null;
    }
}
