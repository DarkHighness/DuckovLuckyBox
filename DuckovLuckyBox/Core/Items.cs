// using HarmonyLib;
// using ItemStatsSystem;
// using Unity.VisualScripting;
// using UnityEngine;

// namespace DuckovLuckyBox
// {
//     public class LuckyBoxItem : Item
//     {
//         public static LuckyBoxItem Instance { get; } = new LuckyBoxItem();
//         public new static int TypeID => 9001;
//         public new static int Order => 9999;
//         public static Texture2D texture = Utils.LoadTexture("item_lucky_box.png");
//         public static Sprite icon = Sprite.Create(
//             texture,
//             new Rect(0, 0, 256, 256),
//             Vector2.zero
//         );

//         public new void Initialize()
//         {
//             base.Initialize();
//             // Additional initialization for LuckyBoxItem
//             AccessTools.Field(typeof(Item), "typeId").SetValue(this, TypeID);
//             AccessTools.Field(typeof(Item), "order").SetValue(this, Order);
//             AccessTools.Field(typeof(Item), "displayName").SetValue(this, "Duckov's Lucky Box");
//             AccessTools.Field(typeof(Item), "description").SetValue(this, "A mysterious box that holds a random surprise inside.");
//             AccessTools.Field(typeof(Item), "texture").SetValue(this, texture);
//             AccessTools.Field(typeof(Item), "icon").SetValue(this, icon);
//         }
//     }

// }
