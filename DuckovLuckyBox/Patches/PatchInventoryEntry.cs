using Cysharp.Threading.Tasks;
using Duckov.UI;
using HarmonyLib;
using ItemStatsSystem;
using UnityEngine.EventSystems;

namespace DuckovLuckyBox.Core
{
    [HarmonyPatch(typeof(InventoryEntry), "OnDrop")]
    public class Patch_InventoryEntry_OnDrop
    {
        public static bool Prefix(PointerEventData eventData, InventoryEntry __instance)
        {
            if (RecycleSessionUI.Instance == null || !RecycleSessionUI.Instance.IsOpen) return true;

            var component = eventData.pointerDrag?.GetComponent<IItemDragSource>();
            if (component == null) return true;

            var item = component.GetItem();
            if (item == null) return true;

            // 如果拖拽到临时背包
            if (__instance.Master.Target == RecycleSessionUI.Instance.ContractInventory)
            {
                if (__instance.Master.Target.GetItemAt(__instance.Index) != null)
                {
                    // 目标位置有物品，不允许合并，直接失败，不消费事件
                    return true; // 让原方法处理，但拖拽将被取消
                }
                else
                {
                    RecycleSessionUI.Instance.AddItemToContractAtAsync(item, __instance.Index).Forget();
                    eventData.Use();
                    return false; // 跳过原方法
                }
            }

            // 如果是从临时背包拖拽
            if (RecycleSessionUI.Instance.IsItemInContract(item))
            {
                var indexInContract = RecycleSessionUI.Instance.GetItemIndexInContract(item);
                var target = __instance.Master.Target;
                var itemInSlot = target.GetItemAt(__instance.Index);
                if (itemInSlot != null)
                {
                    if (itemInSlot.Stackable && itemInSlot.TypeID == item.TypeID && itemInSlot.StackCount + 1 < item.MaxStackCount)
                    {
                        itemInSlot.StackCount += 1;
                        RecycleSessionUI.Instance.RemoveFromContract(item);
                        eventData.Use();
                        item.Detach();
                        item.DestroyTree();
                        return false; // 跳过原方法
                    }
                }
                else
                {
                    item.Detach();
                    if (!target.AddAt(item, __instance.Index))
                    {
                        Log.Error("Failed to add item to target inventory.");
                        // 放回去临时背包
                        RecycleSessionUI.Instance.AddItemToContractAtAsync(item, indexInContract).Forget();
                    }
                    else
                    {
                        RecycleSessionUI.Instance.RemoveFromContract(item);
                    }

                    // 无论成功与否都跳过原方法
                    eventData.Use();
                    return false;
                }
            }

            return true;
        }
    }
}